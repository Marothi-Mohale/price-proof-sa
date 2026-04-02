using PriceProof.Domain.Enums;

namespace PriceProof.Domain.Services;

public sealed class RiskScoringEngine : IRiskScoringEngine
{
    public const string CurrentModelVersion = "risk-rule-v1";

    public RiskScoreResult Calculate(IEnumerable<RiskCaseSignal> cases, DateTimeOffset now)
    {
        var caseList = cases.ToArray();
        var totalCases = caseList.Length;

        if (totalCases == 0)
        {
            return CreateEmptyResult();
        }

        var analyzedCases = caseList
            .Where(signal => signal.AnalysisClassification.HasValue)
            .ToArray();

        var likelyCardSurchargeCases = analyzedCases.Count(signal =>
            signal.AnalysisClassification == DiscrepancyAnalysisClassification.LikelyCardSurcharge);

        var dismissedEquivalentCases = analyzedCases.Count(signal =>
            signal.AnalysisClassification is DiscrepancyAnalysisClassification.Match or DiscrepancyAnalysisClassification.LowerThanQuoted);

        var unclearCases = analyzedCases.Count(signal =>
            signal.AnalysisClassification == DiscrepancyAnalysisClassification.UnclearPositiveMismatch);

        var analyzedCount = analyzedCases.Length;
        var dismissedEquivalentRatio = analyzedCount == 0
            ? 0m
            : decimal.Round((decimal)dismissedEquivalentCases / analyzedCount, 4, MidpointRounding.AwayFromZero);
        var unclearCaseRatio = analyzedCount == 0
            ? 0m
            : decimal.Round((decimal)unclearCases / analyzedCount, 4, MidpointRounding.AwayFromZero);

        decimal confidenceWeightedMismatchTotal = 0m;
        decimal recencyWeightedCaseCount = 0m;

        foreach (var signal in analyzedCases.Where(signal => signal.DifferenceAmount.GetValueOrDefault() > 0m))
        {
            var recencyWeight = GetRecencyWeight(now, signal.AnalysisUpdatedUtc ?? signal.IncidentAtUtc);
            var confidence = Clamp(signal.AnalysisConfidence ?? 0.55m, 0.05m, 1m);
            var positiveDifference = signal.DifferenceAmount!.Value;

            confidenceWeightedMismatchTotal += positiveDifference * confidence * recencyWeight;
            recencyWeightedCaseCount += recencyWeight;
        }

        confidenceWeightedMismatchTotal = decimal.Round(confidenceWeightedMismatchTotal, 2, MidpointRounding.AwayFromZero);
        recencyWeightedCaseCount = decimal.Round(recencyWeightedCaseCount, 4, MidpointRounding.AwayFromZero);

        var caseVolumeScore = Scale(totalCases, 6m, 20m);
        var surchargePatternScore = Scale(likelyCardSurchargeCases, 3m, 45m);
        var mismatchMagnitudeScore = Scale(confidenceWeightedMismatchTotal, 80m, 20m);
        var recencyScore = Scale(recencyWeightedCaseCount, 3m, 15m);

        var score = caseVolumeScore + surchargePatternScore + mismatchMagnitudeScore + recencyScore;

        // The domain model does not yet have an explicit "dismissed" state. We treat
        // Match and LowerThanQuoted outcomes as dismissal-equivalent signals so the
        // score stays conservative until that workflow exists.
        score *= 1m - (dismissedEquivalentRatio * 0.55m);
        score *= 1m - (unclearCaseRatio * 0.20m);

        if (likelyCardSurchargeCases >= 3 && recencyWeightedCaseCount >= 2m)
        {
            score += 5m;
        }

        score = decimal.Round(Clamp(score, 0m, 100m), 2, MidpointRounding.AwayFromZero);

        return new RiskScoreResult(
            CurrentModelVersion,
            totalCases,
            analyzedCount,
            likelyCardSurchargeCases,
            confidenceWeightedMismatchTotal,
            recencyWeightedCaseCount,
            dismissedEquivalentRatio,
            unclearCaseRatio,
            score,
            ToLabel(score));
    }

    private static RiskScoreResult CreateEmptyResult()
    {
        return new RiskScoreResult(
            CurrentModelVersion,
            0,
            0,
            0,
            0m,
            0m,
            0m,
            0m,
            0m,
            RiskLabel.Low);
    }

    private static decimal GetRecencyWeight(DateTimeOffset now, DateTimeOffset occurredAtUtc)
    {
        var ageDays = Math.Max(0d, (now - occurredAtUtc).TotalDays);

        return ageDays switch
        {
            <= 30d => 1m,
            <= 90d => 0.7m,
            <= 180d => 0.4m,
            _ => 0.2m
        };
    }

    private static decimal Scale(decimal value, decimal saturationPoint, decimal maxScore)
    {
        if (value <= 0m || saturationPoint <= 0m || maxScore <= 0m)
        {
            return 0m;
        }

        return decimal.Round(Math.Min(1m, value / saturationPoint) * maxScore, 2, MidpointRounding.AwayFromZero);
    }

    private static decimal Clamp(decimal value, decimal min, decimal max)
    {
        return Math.Min(max, Math.Max(min, value));
    }

    private static RiskLabel ToLabel(decimal score)
    {
        return score switch
        {
            >= 75m => RiskLabel.Severe,
            >= 50m => RiskLabel.High,
            >= 25m => RiskLabel.Moderate,
            _ => RiskLabel.Low
        };
    }
}
