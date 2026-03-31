using FluentValidation;
using PriceProof.Domain.Enums;

namespace PriceProof.Application.PaymentRecords;

public sealed record CreatePaymentRecordRequest(
    Guid CaseId,
    Guid RecordedByUserId,
    PaymentMethod PaymentMethod,
    decimal Amount,
    string CurrencyCode,
    DateTimeOffset PaidAtUtc,
    string? PaymentReference,
    string? MerchantReference,
    string? CardLastFour,
    string? Notes);

public sealed class CreatePaymentRecordRequestValidator : AbstractValidator<CreatePaymentRecordRequest>
{
    public CreatePaymentRecordRequestValidator()
    {
        RuleFor(request => request.CaseId).NotEmpty();
        RuleFor(request => request.RecordedByUserId).NotEmpty();
        RuleFor(request => request.PaymentMethod).IsInEnum();
        RuleFor(request => request.Amount).GreaterThan(0);
        RuleFor(request => request.CurrencyCode).NotEmpty().Length(3);
        RuleFor(request => request.PaymentReference).MaximumLength(64);
        RuleFor(request => request.MerchantReference).MaximumLength(64);
        RuleFor(request => request.CardLastFour).MaximumLength(4);
        RuleFor(request => request.Notes).MaximumLength(2000);
    }
}
