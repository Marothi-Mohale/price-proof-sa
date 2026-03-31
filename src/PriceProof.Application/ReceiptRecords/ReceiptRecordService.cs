using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using PriceProof.Application.Abstractions.Persistence;
using PriceProof.Application.Abstractions.Services;
using PriceProof.Application.Common.Exceptions;
using PriceProof.Domain.Entities;

namespace PriceProof.Application.ReceiptRecords;

internal sealed class ReceiptRecordService : IReceiptRecordService
{
    private readonly IApplicationDbContext _dbContext;

    public ReceiptRecordService(IApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<ReceiptRecordDto> CreateAsync(CreateReceiptRecordRequest request, CancellationToken cancellationToken)
    {
        var paymentRecord = await _dbContext.PaymentRecords
            .Include(entity => entity.Case)
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

        var userExists = await _dbContext.Users
            .AnyAsync(entity => entity.Id == request.UploadedByUserId, cancellationToken);

        if (!userExists)
        {
            throw new NotFoundException($"User '{request.UploadedByUserId}' was not found.");
        }

        var receiptRecord = ReceiptRecord.Create(
            request.CaseId,
            request.PaymentRecordId,
            request.UploadedByUserId,
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
        _dbContext.ReceiptRecords.Add(receiptRecord);
        _dbContext.AuditLogs.Add(AuditLog.Create(
            nameof(ReceiptRecord),
            "ReceiptRecordCreated",
            JsonSerializer.Serialize(request),
            Guid.NewGuid().ToString("N"),
            now,
            request.UploadedByUserId,
            request.CaseId));

        await _dbContext.SaveChangesAsync(cancellationToken);

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
