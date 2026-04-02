using Microsoft.EntityFrameworkCore;
using PriceProof.Application.Abstractions.Diagnostics;
using PriceProof.Application.Abstractions.Persistence;
using PriceProof.Application.Abstractions.Security;
using PriceProof.Application.Abstractions.Services;
using PriceProof.Application.Common;
using PriceProof.Application.Common.Exceptions;
using PriceProof.Domain.Entities;

namespace PriceProof.Application.PaymentRecords;

internal sealed class PaymentRecordService : IPaymentRecordService
{
    private readonly IAuditLogWriter _auditLogWriter;
    private readonly ICurrentUserContext _currentUserContext;
    private readonly IApplicationDbContext _dbContext;

    public PaymentRecordService(IApplicationDbContext dbContext, ICurrentUserContext currentUserContext, IAuditLogWriter auditLogWriter)
    {
        _dbContext = dbContext;
        _currentUserContext = currentUserContext;
        _auditLogWriter = auditLogWriter;
    }

    public async Task<PaymentRecordDto> CreateAsync(CreatePaymentRecordRequest request, CancellationToken cancellationToken)
    {
        request = request with
        {
            CurrencyCode = InputSanitizer.SanitizeCurrencyCode(request.CurrencyCode),
            PaymentReference = InputSanitizer.SanitizeSingleLine(request.PaymentReference, 64),
            MerchantReference = InputSanitizer.SanitizeSingleLine(request.MerchantReference, 64),
            CardLastFour = InputSanitizer.SanitizeSingleLine(request.CardLastFour, 4),
            Notes = InputSanitizer.SanitizeMultiline(request.Notes, 2000)
        };

        var discrepancyCase = await _dbContext.DiscrepancyCases
            .Include(entity => entity.PriceCaptures)
            .SingleOrDefaultAsync(entity => entity.Id == request.CaseId, cancellationToken);

        if (discrepancyCase is null)
        {
            throw new NotFoundException($"Case '{request.CaseId}' was not found.");
        }

        CurrentUserGuards.EnsureCanAccessCase(_currentUserContext, discrepancyCase.ReportedByUserId);
        var currentUserId = CurrentUserGuards.RequireAuthenticatedUserId(_currentUserContext);
        request = request with { RecordedByUserId = currentUserId };

        var paymentRecord = PaymentRecord.Create(
            request.CaseId,
            currentUserId,
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
        _auditLogWriter.Write(
            nameof(PaymentRecord),
            "PaymentRecordCreated",
            request,
            now,
            currentUserId,
            request.CaseId);

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
