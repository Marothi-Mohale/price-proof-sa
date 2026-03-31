using PriceProofSA.Domain.Enums;

namespace PriceProofSA.Domain.ValueObjects;

public sealed record DiscrepancyAnalysis(
    decimal QuotedAmount,
    decimal ChargedAmount,
    decimal DifferenceAmount,
    DiscrepancyClassification Classification,
    bool LikelyUnlawfulCardSurcharge,
    string Explanation);
