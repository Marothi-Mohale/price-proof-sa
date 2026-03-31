namespace PriceProofSA.Domain.Enums;

public enum DiscrepancyClassification
{
    NoMismatch = 0,
    LikelyCardSurcharge = 1,
    PossibleCashback = 2,
    PossibleSeparateServiceFee = 3,
    Unclear = 4
}
