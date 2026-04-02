using Microsoft.EntityFrameworkCore;
using PriceProof.Application.Abstractions.Diagnostics;
using PriceProof.Application.Abstractions.Persistence;
using PriceProof.Application.Abstractions.Security;
using PriceProof.Application.Abstractions.Services;
using PriceProof.Application.Common;
using PriceProof.Application.Common.Exceptions;
using PriceProof.Application.Common.Models;
using PriceProof.Domain.Entities;
using PriceProof.Domain.Services;

namespace PriceProof.Application.Cases;

internal sealed class CaseService : ICaseService
{
    private readonly IAuditLogWriter _auditLogWriter;
    private readonly ICurrentUserContext _currentUserContext;
    private readonly IApplicationDbContext _dbContext;
    private readonly IDiscrepancyDetectionEngine _discrepancyDetectionEngine;
    private readonly IRiskService _riskService;

    public CaseService(
        IApplicationDbContext dbContext,
        ICurrentUserContext currentUserContext,
        IDiscrepancyDetectionEngine discrepancyDetectionEngine,
        IRiskService riskService,
        IAuditLogWriter auditLogWriter)
    {
        _dbContext = dbContext;
        _currentUserContext = currentUserContext;
        _discrepancyDetectionEngine = discrepancyDetectionEngine;
        _riskService = riskService;
        _auditLogWriter = auditLogWriter;
    }

    public async Task<CaseDetailDto> CreateAsync(CreateCaseRequest request, CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var currentUserId = CurrentUserGuards.RequireAuthenticatedUserId(_currentUserContext);

        request = request with
        {
            ReportedByUserId = currentUserId,
            BasketDescription = InputSanitizer.SanitizeRequiredSingleLine(request.BasketDescription, 500),
            CurrencyCode = InputSanitizer.SanitizeCurrencyCode(request.CurrencyCode),
            CustomerReference = InputSanitizer.SanitizeSingleLine(request.CustomerReference, 64),
            Notes = InputSanitizer.SanitizeMultiline(request.Notes, 2000),
            CustomMerchantName = InputSanitizer.SanitizeSingleLine(request.CustomMerchantName, 200)
        };

        var user = await _dbContext.Users
            .SingleOrDefaultAsync(entity => entity.Id == currentUserId, cancellationToken);

        if (user is null)
        {
            throw new NotFoundException($"User '{currentUserId}' was not found.");
        }

        var merchantResolution = await ResolveMerchantAsync(request, now, cancellationToken);

        var discrepancyCase = DiscrepancyCase.Create(
            currentUserId,
            merchantResolution.Merchant.Id,
            merchantResolution.BranchId,
            request.BasketDescription,
            request.IncidentAtUtc,
            request.CurrencyCode,
            request.CustomerReference,
            request.Notes);

        _dbContext.DiscrepancyCases.Add(discrepancyCase);

        if (merchantResolution.CreatedNewMerchant)
        {
            _auditLogWriter.Write(
                nameof(Merchant),
                "MerchantCreatedFromCaseIntake",
                new
                {
                    MerchantId = merchantResolution.Merchant.Id,
                    merchantResolution.Merchant.Name,
                    discrepancyCase.Id
                },
                now,
                currentUserId,
                discrepancyCase.Id);
        }

        _auditLogWriter.Write(
            nameof(DiscrepancyCase),
            "CaseCreated",
            new
            {
                Request = request,
                MerchantId = merchantResolution.Merchant.Id,
                BranchId = merchantResolution.BranchId,
                merchantResolution.CreatedNewMerchant
            },
            now,
            currentUserId,
            discrepancyCase.Id);

        await _dbContext.SaveChangesAsync(cancellationToken);

        discrepancyCase = await LoadCaseAsync(discrepancyCase.Id, cancellationToken);
        return discrepancyCase.ToDetailDto();
    }

    public async Task<CaseDetailDto> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var discrepancyCase = await LoadCaseOrDefaultAsync(id, cancellationToken);

        if (discrepancyCase is null)
        {
            throw new NotFoundException($"Case '{id}' was not found.");
        }

        CurrentUserGuards.EnsureCanAccessCase(_currentUserContext, discrepancyCase.ReportedByUserId);
        return discrepancyCase.ToDetailDto();
    }

    public async Task<PagedResult<CaseSummaryDto>> ListAsync(GetCasesQuery query, CancellationToken cancellationToken)
    {
        var currentUserId = CurrentUserGuards.RequireAuthenticatedUserId(_currentUserContext);
        var take = Math.Clamp(query.Take, 1, 100);
        var skip = Math.Max(0, query.Skip);

        var casesQuery = _dbContext.DiscrepancyCases
            .AsNoTracking()
            .Include(entity => entity.Merchant)
            .Include(entity => entity.Branch)
            .AsQueryable();

        if (!_currentUserContext.IsAdmin)
        {
            casesQuery = casesQuery.Where(entity => entity.ReportedByUserId == currentUserId);
        }

        if (query.MerchantId.HasValue)
        {
            casesQuery = casesQuery.Where(entity => entity.MerchantId == query.MerchantId.Value);
        }

        if (_currentUserContext.IsAdmin && query.ReportedByUserId.HasValue)
        {
            casesQuery = casesQuery.Where(entity => entity.ReportedByUserId == query.ReportedByUserId.Value);
        }

        if (query.Classification.HasValue)
        {
            casesQuery = casesQuery.Where(entity => entity.Classification == query.Classification.Value);
        }

        var totalCount = await casesQuery.CountAsync(cancellationToken);

        var entities = await casesQuery
            .OrderByDescending(entity => entity.UpdatedUtc)
            .ThenByDescending(entity => entity.CreatedUtc)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);

        var items = entities.Select(entity => entity.ToSummaryDto()).ToArray();

        return new PagedResult<CaseSummaryDto>(items, totalCount, skip, take);
    }

    public async Task<CaseAnalysisDto> AnalyzeAsync(Guid id, AnalyzeCaseRequest request, CancellationToken cancellationToken)
    {
        request = request with
        {
            EvidenceText = InputSanitizer.SanitizeMultiline(request.EvidenceText, 2000)
        };

        var discrepancyCase = await _dbContext.DiscrepancyCases
            .Include(entity => entity.PriceCaptures)
            .Include(entity => entity.PaymentRecords)
                .ThenInclude(record => record.ReceiptRecord)
            .SingleOrDefaultAsync(entity => entity.Id == id, cancellationToken);

        if (discrepancyCase is null)
        {
            throw new NotFoundException($"Case '{id}' was not found.");
        }

        CurrentUserGuards.EnsureCanAccessCase(_currentUserContext, discrepancyCase.ReportedByUserId);

        if (!discrepancyCase.LatestQuotedAmount.HasValue || !discrepancyCase.LatestPaidAmount.HasValue)
        {
            throw new ConflictException("A case needs both a quoted amount and a charged amount before analysis can run.");
        }

        var analysis = _discrepancyDetectionEngine.Analyze(new DiscrepancyAnalysisInput(
            discrepancyCase.LatestQuotedAmount.Value,
            discrepancyCase.LatestPaidAmount.Value,
            discrepancyCase.CurrencyCode,
            request.MerchantSaidCardFee,
            request.CashbackPresent,
            request.DeliveryOrServiceFeePresent,
            BuildEvidenceText(discrepancyCase, request.EvidenceText)));

        var now = DateTimeOffset.UtcNow;
        discrepancyCase.ApplyAnalysis(analysis, now);
        await _riskService.RecalculateAsync(discrepancyCase, now, cancellationToken);

        _auditLogWriter.Write(
            nameof(DiscrepancyCase),
            "CaseAnalyzed",
            new
            {
                CaseId = discrepancyCase.Id,
                Request = request,
                Result = new
                {
                    analysis.QuotedAmount,
                    analysis.ChargedAmount,
                    analysis.Difference,
                    analysis.PercentageDifference,
                    Classification = analysis.Classification.ToString(),
                    analysis.Confidence,
                    analysis.Explanation
                }
            },
            now,
            CurrentUserGuards.RequireAuthenticatedUserId(_currentUserContext),
            discrepancyCase.Id);

        await _dbContext.SaveChangesAsync(cancellationToken);

        return new CaseAnalysisDto(
            analysis.QuotedAmount,
            analysis.ChargedAmount,
            analysis.Difference,
            analysis.PercentageDifference,
            analysis.Classification.ToString(),
            analysis.Confidence,
            analysis.Explanation);
    }

    private async Task<DiscrepancyCase> LoadCaseAsync(Guid caseId, CancellationToken cancellationToken)
    {
        return await LoadCaseOrDefaultAsync(caseId, cancellationToken)
               ?? throw new NotFoundException($"Case '{caseId}' was not found.");
    }

    private Task<DiscrepancyCase?> LoadCaseOrDefaultAsync(Guid caseId, CancellationToken cancellationToken)
    {
        return _dbContext.DiscrepancyCases
            .AsNoTracking()
            .Include(entity => entity.ReportedByUser)
            .Include(entity => entity.Merchant)
            .Include(entity => entity.Branch)
            .Include(entity => entity.PriceCaptures)
            .Include(entity => entity.PaymentRecords)
                .ThenInclude(record => record.ReceiptRecord)
            .Include(entity => entity.ComplaintPacks)
            .Include(entity => entity.AuditLogs)
            .SingleOrDefaultAsync(entity => entity.Id == caseId, cancellationToken);
    }

    private static string BuildEvidenceText(DiscrepancyCase discrepancyCase, string? additionalEvidenceText)
    {
        var latestPriceCapture = discrepancyCase.PriceCaptures
            .OrderByDescending(capture => capture.CapturedAtUtc)
            .ThenByDescending(capture => capture.CreatedUtc)
            .FirstOrDefault();

        var latestPayment = discrepancyCase.PaymentRecords
            .OrderByDescending(record => record.PaidAtUtc)
            .ThenByDescending(record => record.CreatedUtc)
            .FirstOrDefault();

        var parts = new[]
        {
            additionalEvidenceText,
            discrepancyCase.Notes,
            latestPriceCapture?.MerchantStatement,
            latestPriceCapture?.Notes,
            latestPayment?.Notes,
            latestPayment?.ReceiptRecord?.MerchantName,
            latestPayment?.ReceiptRecord?.RawText
        };

        return string.Join(
            Environment.NewLine,
            parts.Where(value => !string.IsNullOrWhiteSpace(value)).Select(value => value!.Trim()));
    }

    private async Task<MerchantResolution> ResolveMerchantAsync(
        CreateCaseRequest request,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        if (request.MerchantId.HasValue && request.MerchantId.Value != Guid.Empty)
        {
            var merchant = await _dbContext.Merchants
                .SingleOrDefaultAsync(entity => entity.Id == request.MerchantId.Value, cancellationToken);

            if (merchant is null)
            {
                throw new NotFoundException($"Merchant '{request.MerchantId.Value}' was not found.");
            }

            if (request.BranchId.HasValue)
            {
                var branchExists = await _dbContext.Branches.AnyAsync(
                    entity => entity.Id == request.BranchId.Value && entity.MerchantId == request.MerchantId.Value,
                    cancellationToken);

                if (!branchExists)
                {
                    throw new NotFoundException($"Branch '{request.BranchId.Value}' was not found for merchant '{request.MerchantId.Value}'.");
                }
            }

            return new MerchantResolution(merchant, request.BranchId, CreatedNewMerchant: false);
        }

        if (string.IsNullOrWhiteSpace(request.CustomMerchantName))
        {
            throw new BadRequestException("Choose a merchant or enter a custom merchant.");
        }

        var normalizedMerchantName = request.CustomMerchantName.Trim().ToUpperInvariant();
        var existingMerchant = await _dbContext.Merchants
            .SingleOrDefaultAsync(entity => entity.NormalizedName == normalizedMerchantName, cancellationToken);

        if (existingMerchant is not null)
        {
            return new MerchantResolution(existingMerchant, BranchId: null, CreatedNewMerchant: false);
        }

        var createdMerchant = Merchant.Create(request.CustomMerchantName, category: null, websiteUrl: null);
        createdMerchant.UpdatedUtc = now;
        createdMerchant.CreatedUtc = now;
        _dbContext.Merchants.Add(createdMerchant);

        return new MerchantResolution(createdMerchant, BranchId: null, CreatedNewMerchant: true);
    }

    private sealed record MerchantResolution(Merchant Merchant, Guid? BranchId, bool CreatedNewMerchant);
}
