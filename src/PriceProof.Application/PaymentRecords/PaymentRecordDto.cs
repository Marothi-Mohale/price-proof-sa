namespace PriceProof.Application.PaymentRecords;

public sealed record PaymentRecordDto(
    Guid Id,
    Guid CaseId,
    Guid RecordedByUserId,
    string PaymentMethod,
    decimal Amount,
    string CurrencyCode,
    string? PaymentReference,
    string? MerchantReference,
    string? CardLastFour,
    string? Notes,
    DateTimeOffset PaidAtUtc,
    DateTimeOffset CreatedUtc,
    string CaseClassification,
    decimal? DifferenceAmount);
