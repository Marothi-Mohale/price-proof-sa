using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PriceProofSA.Application.Abstractions.Audit;
using PriceProofSA.Application.Abstractions.Complaints;
using PriceProofSA.Application.Abstractions.Ocr;
using PriceProofSA.Application.Abstractions.Persistence;
using PriceProofSA.Application.Abstractions.Risk;
using PriceProofSA.Application.Abstractions.Storage;
using PriceProofSA.Application.Abstractions.Time;
using PriceProofSA.Application.Cases;
using PriceProofSA.Application.Common.Exceptions;
using PriceProofSA.Domain.Entities;
using PriceProofSA.Domain.Enums;
using PriceProofSA.Domain.Services;

namespace PriceProofSA.Application.Services;

public sealed class CaseService
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IFileStorageService _fileStorageService;
    private readonly IComplaintPackGenerator _complaintPackGenerator;
    private readonly IMerchantRiskScoringService _merchantRiskScoringService;
    private readonly IAuditTrailService _auditTrailService;
    private readonly IClock _clock;
    private readonly DiscrepancyClassifier _classifier;
    private readonly ILogger<CaseService> _logger;

    public CaseService(
        IApplicationDbContext dbContext,
        IFileStorageService fileStorageService,
        IComplaintPackGenerator complaintPackGenerator,
        IMerchantRiskScoringService merchantRiskScoringService,
        IAuditTrailService auditTrailService,
        IClock clock,
        DiscrepancyClassifier classifier,
        ILogger<CaseService> logger)
    {
        _dbContext = dbContext;
        _fileStorageService = fileStorageService;
        _complaintPackGenerator = complaintPackGenerator;
        _merchantRiskScoringService = merchantRiskScoringService;
        _auditTrailService = auditTrailService;
        _clock = clock;
        _classifier = classifier;
        _logger = logger;
    }

    public async Task<CaseDetailDto> CreateCaseAsync(Guid userId, CreateCaseRequest request, CancellationToken cancellationToken = default)
    {
        ValidateCreateCase(request);
        var now = _clock.UtcNow;

        var merchant = await _dbContext.Merchants
            .Include(static item => item.RiskScore)
            .Include(static item => item.Branches)
            .SingleOrDefaultAsync(item => item.NormalizedName == request.MerchantName.Trim().ToLowerInvariant(), cancellationToken);

        if (merchant is null)
        {
            merchant = Merchant.Create(request.MerchantName, request.MerchantCategory, now);
            await _dbContext.Merchants.AddAsync(merchant, cancellationToken);
        }

        Branch? branch = null;
        if (!string.IsNullOrWhiteSpace(request.BranchName))
        {
            branch = merchant.Branches.FirstOrDefault(item =>
                item.Name.Equals(request.BranchName.Trim(), StringComparison.OrdinalIgnoreCase) &&
                string.Equals(item.AddressLine ?? string.Empty, request.BranchAddress?.Trim() ?? string.Empty, StringComparison.OrdinalIgnoreCase));

            if (branch is null)
            {
                branch = merchant.AddBranch(request.BranchName, request.BranchAddress, request.BranchCity, request.BranchProvince, now);
                await _dbContext.Branches.AddAsync(branch, cancellationToken);
            }
        }

        var caseEntity = DiscrepancyCase.Create(userId, merchant.Id, branch?.Id, request.BasketDescription, now);
        await _dbContext.Cases.AddAsync(caseEntity, cancellationToken);

        await AddAuditAsync(
            userId,
            caseEntity.Id,
            nameof(DiscrepancyCase),
            caseEntity.Id,
            "CaseCreated",
            new { request.MerchantName, request.BranchName, request.BasketDescription },
            cancellationToken);

        await _dbContext.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Created case {CaseId} for user {UserId}", caseEntity.Id, userId);

        return await GetCaseAsync(userId, caseEntity.Id, false, cancellationToken)
               ?? throw new AppNotFoundException("Created case could not be loaded.");
    }

    public async Task<IReadOnlyCollection<CaseListItemDto>> ListCasesAsync(Guid userId, bool isAdmin, CancellationToken cancellationToken = default)
    {
        IQueryable<DiscrepancyCase> query = _dbContext.Cases
            .Include(static item => item.Merchant)
            .AsNoTracking();

        if (!isAdmin)
        {
            query = query.Where(item => item.UserId == userId);
        }

        return await query
            .OrderByDescending(item => item.UpdatedAtUtc)
            .Select(item => new CaseListItemDto(
                item.Id,
                item.Merchant!.Name,
                item.BasketDescription,
                item.Status.ToString(),
                item.QuotedAmount,
                item.ChargedAmount,
                item.DifferenceAmount,
                item.Classification.ToString(),
                item.LikelyUnlawfulCardSurcharge,
                item.UpdatedAtUtc))
            .ToListAsync(cancellationToken);
    }

    public async Task<CaseDetailDto?> GetCaseAsync(Guid userId, Guid caseId, bool isAdmin, CancellationToken cancellationToken = default)
    {
        var caseEntity = await LoadCaseAsync(caseId, cancellationToken);
        if (caseEntity is null)
        {
            return null;
        }

        EnsureCaseAccess(caseEntity, userId, isAdmin);
        return MapCase(caseEntity);
    }

    public async Task<CaseDetailDto> AddManualPriceCaptureAsync(Guid userId, Guid caseId, AddManualPriceCaptureRequest request, bool isAdmin, CancellationToken cancellationToken = default)
    {
        if (request.Amount <= 0)
        {
            throw Validation("amount", "Quoted amount must be greater than zero.");
        }

        var caseEntity = await RequireCaseAsync(userId, caseId, isAdmin, cancellationToken);
        var capture = PriceCapture.Create(caseEntity.Id, PriceCaptureMode.ManualEntry, request.Amount, request.QuoteText, request.Notes, _clock.UtcNow);

        caseEntity.AddPriceCapture(capture, _clock.UtcNow);
        await _dbContext.PriceCaptures.AddAsync(capture, cancellationToken);

        await AddAuditAsync(userId, caseEntity.Id, nameof(PriceCapture), capture.Id, "ManualPriceCaptureAdded", request, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return MapCase(caseEntity);
    }

    public async Task<CaseDetailDto> AddMediaPriceCaptureAsync(Guid userId, Guid caseId, AddMediaPriceCaptureRequest request, bool isAdmin, CancellationToken cancellationToken = default)
    {
        ValidateMediaCapture(request);

        var caseEntity = await RequireCaseAsync(userId, caseId, isAdmin, cancellationToken);
        var capture = PriceCapture.Create(caseEntity.Id, request.Mode, request.Amount, request.QuoteText, request.Notes, _clock.UtcNow);

        var storedFile = await _fileStorageService.SaveAsync(
            new FileUploadRequest(request.Content, request.FileName, request.ContentType, "price-evidence", caseEntity.Id.ToString("N")),
            cancellationToken);

        var evidenceType = request.Mode switch
        {
            PriceCaptureMode.AudioQuote => EvidenceFileType.Audio,
            PriceCaptureMode.VideoQuote => EvidenceFileType.Video,
            _ => EvidenceFileType.Image
        };

        var evidence = PriceEvidence.Create(
            capture.Id,
            evidenceType,
            storedFile.FileName,
            storedFile.ContentType,
            storedFile.StoragePath,
            storedFile.SizeBytes,
            storedFile.ContentHash,
            _clock.UtcNow);

        capture.AddEvidence(evidence);
        caseEntity.AddPriceCapture(capture, _clock.UtcNow);

        await _dbContext.PriceCaptures.AddAsync(capture, cancellationToken);
        await _dbContext.PriceEvidence.AddAsync(evidence, cancellationToken);

        await AddAuditAsync(
            userId,
            caseEntity.Id,
            nameof(PriceCapture),
            capture.Id,
            "MediaPriceCaptureAdded",
            new { request.Mode, request.Amount, request.FileName, request.ContentType },
            cancellationToken);

        await _dbContext.SaveChangesAsync(cancellationToken);
        return MapCase(caseEntity);
    }

    public Task<QrQuoteLockStubResponse> GetQrQuoteLockStubAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new QrQuoteLockStubResponse(false, "Merchant QR quote lock support is reserved for a later version of PriceProof SA."));
    }

    public async Task<CaseDetailDto> AddManualPaymentAsync(Guid userId, Guid caseId, AddManualPaymentRequest request, bool isAdmin, CancellationToken cancellationToken = default)
    {
        if (request.Amount <= 0)
        {
            throw Validation("amount", "Charged amount must be greater than zero.");
        }

        var caseEntity = await RequireCaseAsync(userId, caseId, isAdmin, cancellationToken);
        var redactedText = request.Mode == PaymentInputMode.BankNotification
            ? BankNotificationRedactor.Redact(request.BankNotificationText)
            : null;

        var paymentRecord = PaymentRecord.Create(caseEntity.Id, request.Mode, request.Amount, request.IsCardPayment, request.Note, redactedText, _clock.UtcNow);
        caseEntity.AddPaymentRecord(paymentRecord, _clock.UtcNow);

        await _dbContext.PaymentRecords.AddAsync(paymentRecord, cancellationToken);
        AnalyzeCase(caseEntity, paymentRecord.Amount, paymentRecord.IsCardPayment, request.Note, redactedText);

        await AddAuditAsync(
            userId,
            caseEntity.Id,
            nameof(PaymentRecord),
            paymentRecord.Id,
            "ManualPaymentAdded",
            new { request.Amount, request.Mode, request.IsCardPayment },
            cancellationToken);

        await _dbContext.SaveChangesAsync(cancellationToken);
        await _merchantRiskScoringService.RecalculateAsync(caseEntity.MerchantId, cancellationToken);

        return MapCase(caseEntity);
    }

    public async Task<(CaseDetailDto Case, Guid ReceiptId)> AddReceiptPaymentAsync(Guid userId, Guid caseId, AddReceiptPaymentRequest request, bool isAdmin, CancellationToken cancellationToken = default)
    {
        ValidateReceiptPayment(request);

        var caseEntity = await RequireCaseAsync(userId, caseId, isAdmin, cancellationToken);
        var paymentRecord = PaymentRecord.Create(caseEntity.Id, PaymentInputMode.ReceiptUpload, request.EnteredAmount, request.IsCardPayment, request.Note, null, _clock.UtcNow);

        var storedFile = await _fileStorageService.SaveAsync(
            new FileUploadRequest(request.Content, request.FileName, request.ContentType, "receipts", caseEntity.Id.ToString("N")),
            cancellationToken);

        var receipt = ReceiptRecord.Create(
            paymentRecord.Id,
            storedFile.FileName,
            storedFile.ContentType,
            storedFile.StoragePath,
            storedFile.SizeBytes,
            storedFile.ContentHash);

        paymentRecord.AttachReceipt(receipt);
        caseEntity.AddPaymentRecord(paymentRecord, _clock.UtcNow);

        await _dbContext.PaymentRecords.AddAsync(paymentRecord, cancellationToken);
        await _dbContext.ReceiptRecords.AddAsync(receipt, cancellationToken);

        if (paymentRecord.Amount.HasValue)
        {
            AnalyzeCase(caseEntity, paymentRecord.Amount, paymentRecord.IsCardPayment, request.Note, receipt.OcrRawText);
        }

        await AddAuditAsync(
            userId,
            caseEntity.Id,
            nameof(ReceiptRecord),
            receipt.Id,
            "ReceiptUploaded",
            new { request.FileName, request.ContentType, request.EnteredAmount },
            cancellationToken);

        await _dbContext.SaveChangesAsync(cancellationToken);
        return (MapCase(caseEntity), receipt.Id);
    }

    public async Task<CaseDetailDto> ApplyReceiptOcrResultAsync(Guid receiptId, OcrDocumentResult result, CancellationToken cancellationToken = default)
    {
        var receipt = await _dbContext.ReceiptRecords
            .Include(static item => item.PaymentRecord!)
            .ThenInclude(static payment => payment.Case!)
            .ThenInclude(static item => item.Merchant!)
            .ThenInclude(static merchant => merchant.RiskScore)
            .Include(static item => item.PaymentRecord!)
            .ThenInclude(static payment => payment.Case!)
            .ThenInclude(static item => item.Branch)
            .Include(static item => item.PaymentRecord!)
            .ThenInclude(static payment => payment.Case!)
            .ThenInclude(static item => item.PriceCaptures)
            .ThenInclude(static item => item.Evidence)
            .Include(static item => item.PaymentRecord!)
            .ThenInclude(static payment => payment.Case!)
            .ThenInclude(static item => item.PaymentRecords)
            .ThenInclude(static item => item.ReceiptRecord)
            .Include(static item => item.PaymentRecord!)
            .ThenInclude(static payment => payment.Case!)
            .ThenInclude(static item => item.ComplaintPacks)
            .SingleOrDefaultAsync(item => item.Id == receiptId, cancellationToken)
            ?? throw new AppNotFoundException("Receipt could not be found.");

        var now = _clock.UtcNow;

        if (result.NoProviderConfigured)
        {
            receipt.MarkNoProvider(now);
        }
        else if (!result.Success)
        {
            receipt.MarkFailed(now, result.RawText);
        }
        else
        {
            receipt.CompleteOcr(result.RawText, result.ParsedAmount, now);
            if (result.ParsedAmount.HasValue)
            {
                receipt.PaymentRecord!.ResolveAmount(result.ParsedAmount.Value);
            }
        }

        var caseEntity = receipt.PaymentRecord!.Case!;
        if (receipt.PaymentRecord.Amount.HasValue)
        {
            AnalyzeCase(caseEntity, receipt.PaymentRecord.Amount, receipt.PaymentRecord.IsCardPayment, receipt.PaymentRecord.Note, receipt.OcrRawText);
        }

        await AddAuditAsync(
            caseEntity.UserId,
            caseEntity.Id,
            nameof(ReceiptRecord),
            receipt.Id,
            result.Success ? "ReceiptOcrCompleted" : "ReceiptOcrFailed",
            new { result.ProviderName, result.Message, result.ParsedAmount },
            cancellationToken);

        await _dbContext.SaveChangesAsync(cancellationToken);
        await _merchantRiskScoringService.RecalculateAsync(caseEntity.MerchantId, cancellationToken);

        return MapCase(caseEntity);
    }

    public async Task<CaseDetailDto> GenerateComplaintPackAsync(Guid userId, Guid caseId, bool isAdmin, CancellationToken cancellationToken = default)
    {
        var caseEntity = await RequireCaseAsync(userId, caseId, isAdmin, cancellationToken);
        if (!caseEntity.QuotedAmount.HasValue || !caseEntity.ChargedAmount.HasValue || !caseEntity.DifferenceAmount.HasValue)
        {
            throw Validation("case", "A completed discrepancy analysis is required before generating a complaint pack.");
        }

        var attachments = caseEntity.PriceCaptures
            .SelectMany(capture => capture.Evidence.Select(evidence => new ComplaintAttachment(
                $"{capture.Mode} evidence",
                evidence.FileName,
                evidence.ContentType,
                evidence.StoragePath,
                evidence.UploadedAtUtc)))
            .Concat(caseEntity.PaymentRecords
                .Where(record => record.ReceiptRecord is not null)
                .Select(record => record.ReceiptRecord!)
                .Select(receipt => new ComplaintAttachment(
                    "Receipt upload",
                    receipt.FileName,
                    receipt.ContentType,
                    receipt.StoragePath,
                    receipt.ProcessedAtUtc ?? receipt.PaymentRecord!.CapturedAtUtc)))
            .ToArray();

        var generatedDocument = await _complaintPackGenerator.GenerateAsync(
            new ComplaintPackBuildRequest(
                caseEntity.Id,
                caseEntity.Merchant!.Name,
                caseEntity.BasketDescription,
                caseEntity.QuotedAmount.Value,
                caseEntity.ChargedAmount.Value,
                caseEntity.DifferenceAmount.Value,
                caseEntity.Classification.ToString(),
                caseEntity.ComplaintSummary ?? "No summary available.",
                caseEntity.CreatedAtUtc,
                caseEntity.UpdatedAtUtc,
                attachments),
            cancellationToken);

        await using var contentStream = new MemoryStream(generatedDocument.Content, writable: false);
        var storedFile = await _fileStorageService.SaveAsync(
            new FileUploadRequest(contentStream, generatedDocument.FileName, generatedDocument.ContentType, "complaint-packs", caseEntity.Id.ToString("N")),
            cancellationToken);

        var complaintPack = ComplaintPack.Create(caseEntity.Id, storedFile.FileName, storedFile.StoragePath, generatedDocument.Summary, storedFile.ContentHash, _clock.UtcNow);
        caseEntity.AttachComplaintPack(complaintPack, _clock.UtcNow);

        await _dbContext.ComplaintPacks.AddAsync(complaintPack, cancellationToken);
        await AddAuditAsync(userId, caseEntity.Id, nameof(ComplaintPack), complaintPack.Id, "ComplaintPackGenerated", new { complaintPack.FileName }, cancellationToken);

        await _dbContext.SaveChangesAsync(cancellationToken);
        return MapCase(caseEntity);
    }

    public async Task<StoredFileDownload> DownloadComplaintPackAsync(Guid userId, Guid caseId, bool isAdmin, CancellationToken cancellationToken = default)
    {
        var caseEntity = await RequireCaseAsync(userId, caseId, isAdmin, cancellationToken);
        var latestPack = caseEntity.ComplaintPacks.OrderByDescending(item => item.GeneratedAtUtc).FirstOrDefault()
            ?? throw new AppNotFoundException("No complaint pack exists for this case.");

        return await _fileStorageService.OpenReadAsync(latestPack.StoragePath, cancellationToken);
    }

    private async Task<DiscrepancyCase?> LoadCaseAsync(Guid caseId, CancellationToken cancellationToken)
    {
        return await _dbContext.Cases
            .Include(static item => item.Merchant!)
            .ThenInclude(static merchant => merchant.RiskScore)
            .Include(static item => item.Branch)
            .Include(static item => item.PriceCaptures)
            .ThenInclude(static capture => capture.Evidence)
            .Include(static item => item.PaymentRecords)
            .ThenInclude(static payment => payment.ReceiptRecord)
            .Include(static item => item.ComplaintPacks)
            .SingleOrDefaultAsync(item => item.Id == caseId, cancellationToken);
    }

    private async Task<DiscrepancyCase> RequireCaseAsync(Guid userId, Guid caseId, bool isAdmin, CancellationToken cancellationToken)
    {
        var caseEntity = await LoadCaseAsync(caseId, cancellationToken)
            ?? throw new AppNotFoundException("Case could not be found.");

        EnsureCaseAccess(caseEntity, userId, isAdmin);
        return caseEntity;
    }

    private void AnalyzeCase(DiscrepancyCase caseEntity, decimal? chargedAmount, bool isCardPayment, params string?[] context)
    {
        var quotedAmount = caseEntity.QuotedAmount ?? caseEntity.PriceCaptures
            .OrderByDescending(item => item.CapturedAtUtc)
            .FirstOrDefault(item => item.CapturedAmount.HasValue)
            ?.CapturedAmount;

        if (!quotedAmount.HasValue || !chargedAmount.HasValue)
        {
            return;
        }

        var analysis = _classifier.Analyze(quotedAmount.Value, chargedAmount.Value, isCardPayment, context);
        caseEntity.ApplyAnalysis(analysis, _clock.UtcNow);
    }

    private async Task AddAuditAsync(Guid? userId, Guid? caseId, string entityType, Guid entityId, string action, object payload, CancellationToken cancellationToken)
    {
        var audit = await _auditTrailService.BuildEntryAsync(
            new AuditEntryInput(userId, caseId, entityType, entityId, action, payload),
            cancellationToken);

        await _dbContext.AuditLogs.AddAsync(audit, cancellationToken);
    }

    private static void EnsureCaseAccess(DiscrepancyCase caseEntity, Guid userId, bool isAdmin)
    {
        if (!isAdmin && caseEntity.UserId != userId)
        {
            throw new UnauthorizedAppException("You do not have access to this case.");
        }
    }

    private static void ValidateCreateCase(CreateCaseRequest request)
    {
        var errors = new Dictionary<string, string[]>();

        if (string.IsNullOrWhiteSpace(request.MerchantName))
        {
            errors["merchantName"] = ["Merchant name is required."];
        }

        if (string.IsNullOrWhiteSpace(request.BasketDescription))
        {
            errors["basketDescription"] = ["Basket description is required."];
        }

        if (errors.Count > 0)
        {
            throw new InputValidationException("Case creation failed validation.", errors);
        }
    }

    private static void ValidateMediaCapture(AddMediaPriceCaptureRequest request)
    {
        var errors = new Dictionary<string, string[]>();

        if (request.Mode is PriceCaptureMode.ManualEntry or PriceCaptureMode.MerchantQrQuoteLock)
        {
            errors["mode"] = ["Only shelf photo, audio quote, or video quote are valid media capture modes."];
        }

        if (string.IsNullOrWhiteSpace(request.FileName))
        {
            errors["fileName"] = ["A media file is required."];
        }

        if (string.IsNullOrWhiteSpace(request.ContentType))
        {
            errors["contentType"] = ["A content type is required."];
        }

        if (errors.Count > 0)
        {
            throw new InputValidationException("Media capture failed validation.", errors);
        }
    }

    private static void ValidateReceiptPayment(AddReceiptPaymentRequest request)
    {
        var errors = new Dictionary<string, string[]>();

        if (string.IsNullOrWhiteSpace(request.FileName))
        {
            errors["fileName"] = ["A receipt file is required."];
        }

        if (string.IsNullOrWhiteSpace(request.ContentType))
        {
            errors["contentType"] = ["A content type is required."];
        }

        if (errors.Count > 0)
        {
            throw new InputValidationException("Receipt upload failed validation.", errors);
        }
    }

    private static InputValidationException Validation(string field, string message)
    {
        return new InputValidationException("Validation failed.", new Dictionary<string, string[]>
        {
            [field] = [message]
        });
    }

    private static CaseDetailDto MapCase(DiscrepancyCase caseEntity)
    {
        var latestComplaintPack = caseEntity.ComplaintPacks
            .OrderByDescending(item => item.GeneratedAtUtc)
            .Select(item => new ComplaintPackDto(item.Id, item.FileName, item.StoragePath, item.Summary, item.GeneratedAtUtc))
            .FirstOrDefault();

        var merchantRisk = caseEntity.Merchant?.RiskScore is null
            ? null
            : new MerchantRiskDto(
                caseEntity.Merchant.RiskScore.TotalReports,
                caseEntity.Merchant.RiskScore.ConfirmedSurchargeSignals,
                caseEntity.Merchant.RiskScore.Score,
                caseEntity.Merchant.RiskScore.Trend,
                caseEntity.Merchant.RiskScore.LastCalculatedAtUtc);

        return new CaseDetailDto(
            caseEntity.Id,
            caseEntity.MerchantId,
            caseEntity.Merchant?.Name ?? "Unknown merchant",
            caseEntity.Branch is null
                ? null
                : new BranchDto(caseEntity.Branch.Id, caseEntity.Branch.Name, caseEntity.Branch.AddressLine, caseEntity.Branch.City, caseEntity.Branch.Province),
            caseEntity.BasketDescription,
            caseEntity.Status.ToString(),
            caseEntity.QuotedAmount,
            caseEntity.ChargedAmount,
            caseEntity.DifferenceAmount,
            caseEntity.Classification.ToString(),
            caseEntity.LikelyUnlawfulCardSurcharge,
            caseEntity.ComplaintSummary,
            caseEntity.CreatedAtUtc,
            caseEntity.UpdatedAtUtc,
            caseEntity.PriceCaptures
                .OrderByDescending(item => item.CapturedAtUtc)
                .Select(item => new PriceCaptureDto(
                    item.Id,
                    item.Mode.ToString(),
                    item.CapturedAmount,
                    item.QuoteText,
                    item.Notes,
                    item.CapturedAtUtc,
                    item.Evidence
                        .Select(evidence => new PriceEvidenceDto(
                            evidence.Id,
                            evidence.FileType.ToString(),
                            evidence.FileName,
                            evidence.ContentType,
                            evidence.StoragePath,
                            evidence.UploadedAtUtc))
                        .ToArray()))
                .ToArray(),
            caseEntity.PaymentRecords
                .OrderByDescending(item => item.CapturedAtUtc)
                .Select(item => new PaymentRecordDto(
                    item.Id,
                    item.Mode.ToString(),
                    item.Amount,
                    item.IsCardPayment,
                    item.Note,
                    item.RedactedBankNotificationText,
                    item.CapturedAtUtc,
                    item.ReceiptRecord is null
                        ? null
                        : new ReceiptRecordDto(
                            item.ReceiptRecord.Id,
                            item.ReceiptRecord.FileName,
                            item.ReceiptRecord.OcrStatus.ToString(),
                            item.ReceiptRecord.ParsedTotalAmount,
                            item.ReceiptRecord.ProcessedAtUtc,
                            item.ReceiptRecord.RetryCount)))
                .ToArray(),
            latestComplaintPack,
            merchantRisk);
    }
}
