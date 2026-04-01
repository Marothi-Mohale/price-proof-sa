using PriceProof.Domain.Enums;

namespace PriceProof.Domain.Services;

public sealed record DiscrepancyAnalysisInput(
    decimal QuotedAmount,
    decimal ChargedAmount,
    string CurrencyCode,
    bool MerchantSaidCardFee = false,
    bool CashbackPresent = false,
    bool DeliveryOrServiceFeePresent = false,
    string? EvidenceText = null);

public sealed record DiscrepancyAnalysisResult(
    decimal QuotedAmount,
    decimal ChargedAmount,
    decimal Difference,
    decimal? PercentageDifference,
    DiscrepancyAnalysisClassification Classification,
    decimal Confidence,
    string Explanation);
