namespace PriceProof.Application.Risk;

public sealed record RiskSnapshotDto(
    string ModelVersion,
    int TotalCases,
    int AnalyzedCases,
    int LikelyCardSurchargeCases,
    decimal ConfidenceWeightedMismatchTotal,
    decimal RecencyWeightedCaseCount,
    decimal DismissedEquivalentRatio,
    decimal UnclearCaseRatio,
    decimal Score,
    string Label,
    DateTimeOffset CalculatedUtc);

public sealed record MerchantRiskDto(
    Guid MerchantId,
    string MerchantName,
    string? Category,
    string? WebsiteUrl,
    decimal Score,
    string Label,
    int TotalCases,
    int AnalyzedCases,
    int LikelyCardSurchargeCases,
    decimal ConfidenceWeightedMismatchTotal,
    decimal RecencyWeightedCaseCount,
    decimal DismissedEquivalentRatio,
    decimal UnclearCaseRatio,
    DateTimeOffset? CalculatedUtc,
    IReadOnlyCollection<RiskSnapshotDto> Snapshots);

public sealed record BranchRiskDto(
    Guid BranchId,
    Guid MerchantId,
    string BranchName,
    string MerchantName,
    string City,
    string Province,
    decimal Score,
    string Label,
    int TotalCases,
    int AnalyzedCases,
    int LikelyCardSurchargeCases,
    decimal ConfidenceWeightedMismatchTotal,
    decimal RecencyWeightedCaseCount,
    decimal DismissedEquivalentRatio,
    decimal UnclearCaseRatio,
    DateTimeOffset? CalculatedUtc,
    IReadOnlyCollection<RiskSnapshotDto> Snapshots);

public sealed record RiskLeaderboardMerchantDto(
    Guid MerchantId,
    string MerchantName,
    string? Category,
    decimal Score,
    string Label,
    int TotalCases,
    int LikelyCardSurchargeCases,
    DateTimeOffset? CalculatedUtc);

public sealed record RiskLeaderboardBranchDto(
    Guid BranchId,
    Guid MerchantId,
    string BranchName,
    string MerchantName,
    string City,
    string Province,
    decimal Score,
    string Label,
    int TotalCases,
    int LikelyCardSurchargeCases,
    DateTimeOffset? CalculatedUtc);

public sealed record RiskOverviewDto(
    IReadOnlyCollection<RiskLeaderboardMerchantDto> TopMerchants,
    IReadOnlyCollection<RiskLeaderboardBranchDto> TopBranches);
