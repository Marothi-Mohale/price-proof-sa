using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using PriceProof.Application.Abstractions.Persistence;
using PriceProof.Application.Abstractions.Services;
using PriceProof.Application.Common.Exceptions;
using PriceProof.Domain.Entities;

namespace PriceProof.Application.PaymentRecords;

internal sealed class PaymentRecordService : IPaymentRecordService
{
    private readonly IApplicationDbContext _dbContext;

    public PaymentRecordService(IApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<PaymentRecordDto> CreateAsync(CreatePaymentRecordRequest request, CancellationToken cancellationToken)
    {
        var discrepancyCase = await _dbContext.DiscrepancyCases
            .Include(entity => entity.PriceCaptures)
            .SingleOrDefaultAsync(entity => entity.Id == request.CaseId, cancellationToken);

        if (discrepancyCase is null)
        {
            throw new NotFoundException($"Case '{request.CaseId}' was not found.");
        }

        var userExists = await _dbContext.Users
            .AnyAsync(entity => entity.Id == request.RecordedByUserId, cancellationToken);

        if (!userExists)
        {
            throw new NotFoundException($"User '{request.RecordedByUserId}' was not found.");
        }

        var paymentRecord = PaymentRecord.Create(
            request.CaseId,
            request.RecordedByUserId,
            request.PaymentMethod,
            request.Amount,
            request.CurrencyCode,
            request.PaidAtUtc,
            request.PaymentReference,
            request.MerchantReference,
            request.CardLastFour,
            request.Notes);

        var now = DateTimeOffset.UtcNow;
        discrepancyCase.AddPaymentRecord(paymentRecord, now);
        _dbContext.PaymentRecords.Add(paymentRecord);
        _dbContext.AuditLogs.Add(AuditLog.Create(
            nameof(PaymentRecord),
            "PaymentRecordCreated",
            JsonSerializer.Serialize(request),
            Guid.NewGuid().ToString("N"),
            now,
            request.RecordedByUserId,
            request.CaseId));

        await _dbContext.SaveChangesAsync(cancellationToken);

        return new PaymentRecordDto(
            paymentRecord.Id,
            paymentRecord.CaseId,
            paymentRecord.RecordedByUserId,
            paymentRecord.PaymentMethod.ToString(),
            paymentRecord.Amount,
            paymentRecord.CurrencyCode,
            paymentRecord.PaymentReference,
            paymentRecord.MerchantReference,
            paymentRecord.CardLastFour,
            paymentRecord.Notes,
            paymentRecord.PaidAtUtc,
            paymentRecord.CreatedUtc,
            discrepancyCase.Classification.ToString(),
            discrepancyCase.DifferenceAmount);
    }
}
