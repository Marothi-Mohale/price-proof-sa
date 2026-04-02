using PriceProof.Domain.Enums;

namespace PriceProof.Domain.Services;

public sealed record RiskCaseSignal(
    Guid CaseId,
    DiscrepancyAnalysisClassification? AnalysisClassification,
    decimal? AnalysisConfidence,
    decimal? DifferenceAmount,
    DateTimeOffset IncidentAtUtc,
    DateTimeOffset? AnalysisUpdatedUtc);

public sealed record RiskScoreResult(
    string ModelVersion,
    int TotalCases,
    int AnalyzedCases,
    int LikelyCardSurchargeCases,
    decimal ConfidenceWeightedMismatchTotal,
    decimal RecencyWeightedCaseCount,
    decimal DismissedEquivalentRatio,
    decimal UnclearCaseRatio,
    decimal Score,
    RiskLabel Label);
