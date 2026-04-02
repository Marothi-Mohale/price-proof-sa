using System.Text.Json;
using PriceProof.Application.Abstractions.Ocr;
using Microsoft.EntityFrameworkCore;
using PriceProof.Application.Abstractions.Diagnostics;
using PriceProof.Application.Abstractions.Persistence;
using PriceProof.Application.Abstractions.Security;
using PriceProof.Application.Abstractions.Services;
using PriceProof.Application.Common;
using PriceProof.Application.Common.Exceptions;
using PriceProof.Domain.Entities;

namespace PriceProof.Application.ReceiptRecords;

internal sealed class ReceiptRecordService : IReceiptRecordService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly IAuditLogWriter _auditLogWriter;
    private readonly ICurrentUserContext _currentUserContext;
    private readonly IApplicationDbContext _dbContext;
    private readonly IReceiptDocumentContentResolver _documentContentResolver;
    private readonly IOcrOrchestrator _ocrOrchestrator;

    public ReceiptRecordService(
        IApplicationDbContext dbContext,
        ICurrentUserContext currentUserContext,
        IReceiptDocumentContentResolver documentContentResolver,
        IOcrOrchestrator ocrOrchestrator,
        IAuditLogWriter auditLogWriter)
    {
        _dbContext = dbContext;
        _currentUserContext = currentUserContext;
        _documentContentResolver = documentContentResolver;
        _ocrOrchestrator = ocrOrchestrator;
        _auditLogWriter = auditLogWriter;
    }

    public async Task<ReceiptRecordDto> CreateAsync(CreateReceiptRecordRequest request, CancellationToken cancellationToken)
    {
        request = request with
        {
            FileName = InputSanitizer.SanitizeRequiredSingleLine(request.FileName, 260),
            ContentType = InputSanitizer.SanitizeRequiredSingleLine(request.ContentType, 120),
            StoragePath = InputSanitizer.SanitizeRequiredSingleLine(request.StoragePath, 500),
            CurrencyCode = InputSanitizer.SanitizeCurrencyCode(request.CurrencyCode),
            ReceiptNumber = InputSanitizer.SanitizeSingleLine(request.ReceiptNumber, 64),
            MerchantName = InputSanitizer.SanitizeSingleLine(request.MerchantName, 200),
            RawText = InputSanitizer.SanitizeMultiline(request.RawText, 16000),
            FileHash = InputSanitizer.SanitizeHash(request.FileHash, 128)
        };

        var paymentRecord = await _dbContext.PaymentRecords
            .Include(entity => entity.Case)
            .Include(entity => entity.ReceiptRecord)
            .SingleOrDefaultAsync(entity => entity.Id == request.PaymentRecordId, cancellationToken);

        if (paymentRecord is null)
        {
            throw new NotFoundException($"Payment record '{request.PaymentRecordId}' was not found.");
        }

        if (paymentRecord.CaseId != request.CaseId)
        {
            throw new ConflictException("The supplied payment record does not belong to the supplied case.");
        }

        if (paymentRecord.ReceiptRecord is not null)
        {
            throw new ConflictException("A receipt has already been attached to this payment record.");
        }

        if (paymentRecord.Case is null)
        {
            throw new NotFoundException($"Case '{request.CaseId}' was not found.");
        }

        CurrentUserGuards.EnsureCanAccessCase(_currentUserContext, paymentRecord.Case.ReportedByUserId);
        var currentUserId = CurrentUserGuards.RequireAuthenticatedUserId(_currentUserContext);
        request = request with { UploadedByUserId = currentUserId };

        var receiptRecord = ReceiptRecord.Create(
            request.CaseId,
            request.PaymentRecordId,
            currentUserId,
            request.EvidenceType,
            request.FileName,
            request.ContentType,
            request.StoragePath,
            request.UploadedAtUtc,
            request.CurrencyCode,
            request.ParsedTotalAmount,
            request.ReceiptNumber,
            request.MerchantName,
            request.RawText,
            request.FileHash);

        var now = DateTimeOffset.UtcNow;
        paymentRecord.AttachReceipt(receiptRecord, now);
        paymentRecord.Case?.MarkReceiptReceived(now);
        _dbContext.ReceiptRecords.Add(receiptRecord);
        _auditLogWriter.Write(
            nameof(ReceiptRecord),
            "ReceiptRecordCreated",
            request,
            now,
            currentUserId,
            request.CaseId);

        await _dbContext.SaveChangesAsync(cancellationToken);

        return ToDto(receiptRecord);
    }

    public async Task<RunReceiptOcrResultDto> RunOcrAsync(Guid id, CancellationToken cancellationToken)
    {
        var receiptRecord = await _dbContext.ReceiptRecords
            .SingleOrDefaultAsync(entity => entity.Id == id, cancellationToken);

        if (receiptRecord is null)
        {
            throw new NotFoundException($"Receipt record '{id}' was not found.");
        }

        var caseOwnerUserId = await _dbContext.DiscrepancyCases
            .AsNoTracking()
            .Where(entity => entity.Id == receiptRecord.CaseId)
            .Select(entity => entity.ReportedByUserId)
            .SingleOrDefaultAsync(cancellationToken);

        if (caseOwnerUserId == Guid.Empty)
        {
            throw new NotFoundException($"Case '{receiptRecord.CaseId}' was not found.");
        }

        CurrentUserGuards.EnsureCanAccessCase(_currentUserContext, caseOwnerUserId);

        var document = await _documentContentResolver.ResolveAsync(
            receiptRecord.FileName,
            receiptRecord.ContentType,
            receiptRecord.StoragePath,
            cancellationToken);

        var ocrResult = await _ocrOrchestrator.RecognizeReceiptAsync(document, cancellationToken);
        var lineItemsJson = JsonSerializer.Serialize(ocrResult.LineItems, JsonOptions);
        var processedAtUtc = DateTimeOffset.UtcNow;

        receiptRecord.ApplyOcrResult(
            ocrResult.ProviderName,
            ocrResult.Confidence,
            ocrResult.RawPayloadMetadataJson,
            processedAtUtc,
            ocrResult.RawText,
            ocrResult.MerchantName,
            ocrResult.TransactionTotal,
            ocrResult.TransactionAtUtc,
            ocrResult.ReceiptNumber,
            lineItemsJson);

        _auditLogWriter.Write(
            nameof(ReceiptRecord),
            "ReceiptOcrCompleted",
            new
            {
                ReceiptRecordId = receiptRecord.Id,
                receiptRecord.CaseId,
                Provider = ocrResult.ProviderName,
                ocrResult.Confidence,
                ocrResult.MerchantName,
                ocrResult.TransactionTotal,
                ocrResult.TransactionAtUtc,
                LineItemCount = ocrResult.LineItems.Count
            },
            processedAtUtc,
            CurrentUserGuards.RequireAuthenticatedUserId(_currentUserContext),
            receiptRecord.CaseId);

        await _dbContext.SaveChangesAsync(cancellationToken);

        return new RunReceiptOcrResultDto(
            receiptRecord.Id,
            ocrResult.ProviderName,
            ocrResult.Confidence,
            ocrResult.RawPayloadMetadataJson,
            receiptRecord.MerchantName,
            receiptRecord.ParsedTotalAmount,
            receiptRecord.TransactionAtUtc,
            ocrResult.LineItems
                .Select(item => new ReceiptOcrLineItemDto(item.Description, item.TotalAmount, item.Quantity, item.UnitPrice))
                .ToArray(),
            receiptRecord.RawText,
            processedAtUtc);
    }

    private static ReceiptRecordDto ToDto(ReceiptRecord receiptRecord)
    {
        return new ReceiptRecordDto(
            receiptRecord.Id,
            receiptRecord.CaseId,
            receiptRecord.PaymentRecordId,
            receiptRecord.UploadedByUserId,
            receiptRecord.EvidenceType.ToString(),
            receiptRecord.FileName,
            receiptRecord.ContentType,
            receiptRecord.StoragePath,
            receiptRecord.CurrencyCode,
            receiptRecord.ParsedTotalAmount,
            receiptRecord.ReceiptNumber,
            receiptRecord.MerchantName,
            receiptRecord.RawText,
            receiptRecord.UploadedAtUtc,
            receiptRecord.CreatedUtc);
    }
}
