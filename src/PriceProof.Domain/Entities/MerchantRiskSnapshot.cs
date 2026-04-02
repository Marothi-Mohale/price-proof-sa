using PriceProof.Domain.Common;
using PriceProof.Domain.Enums;
using PriceProof.Domain.Services;

namespace PriceProof.Domain.Entities;

public sealed class MerchantRiskSnapshot : AuditableEntity
{
    private MerchantRiskSnapshot()
    {
    }

    public Guid MerchantId { get; private set; }

    public Merchant? Merchant { get; private set; }

    public string ModelVersion { get; private set; } = string.Empty;

    public int TotalCases { get; private set; }

    public int AnalyzedCases { get; private set; }

    public int LikelyCardSurchargeCases { get; private set; }

    public decimal ConfidenceWeightedMismatchTotal { get; private set; }

    public decimal RecencyWeightedCaseCount { get; private set; }

    public decimal DismissedEquivalentRatio { get; private set; }

    public decimal UnclearCaseRatio { get; private set; }

    public decimal Score { get; private set; }

    public RiskLabel Label { get; private set; }

    public DateTimeOffset CalculatedUtc { get; private set; }

    public Guid? TriggeredByCaseId { get; private set; }

    public static MerchantRiskSnapshot Create(
        Guid merchantId,
        RiskScoreResult score,
        DateTimeOffset calculatedUtc,
        Guid? triggeredByCaseId = null)
    {
        return new MerchantRiskSnapshot
        {
            MerchantId = merchantId,
            ModelVersion = score.ModelVersion,
            TotalCases = score.TotalCases,
            AnalyzedCases = score.AnalyzedCases,
            LikelyCardSurchargeCases = score.LikelyCardSurchargeCases,
            ConfidenceWeightedMismatchTotal = score.ConfidenceWeightedMismatchTotal,
            RecencyWeightedCaseCount = score.RecencyWeightedCaseCount,
            DismissedEquivalentRatio = score.DismissedEquivalentRatio,
            UnclearCaseRatio = score.UnclearCaseRatio,
            Score = score.Score,
            Label = score.Label,
            CalculatedUtc = calculatedUtc,
            TriggeredByCaseId = triggeredByCaseId,
            CreatedUtc = calculatedUtc,
            UpdatedUtc = calculatedUtc
        };
    }
}
