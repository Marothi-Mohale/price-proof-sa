namespace PriceProofSA.Application.Abstractions.Risk;

public sealed record MerchantRiskSummary(
    Guid MerchantId,
    int TotalReports,
    int ConfirmedSurchargeSignals,
    decimal Score,
    string Trend,
    DateTimeOffset LastCalculatedAtUtc);
