namespace PriceProof.Domain.Enums;

public enum DiscrepancyAnalysisClassification
{
    Match = 1,
    LikelyCardSurcharge = 2,
    PossibleCashback = 3,
    PossibleSeparateFee = 4,
    UnclearPositiveMismatch = 5,
    LowerThanQuoted = 6
}
