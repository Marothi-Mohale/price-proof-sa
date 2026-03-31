namespace PriceProofSA.Domain.ValueObjects;

public sealed record MerchantRiskSnapshot(
    int TotalReports,
    int ConfirmedSurchargeSignals,
    decimal Score,
    string Trend);
