namespace PriceProof.Application.Admin;

public sealed record ClassificationCountDto(
    string Classification,
    int Count);

public sealed record AdminMerchantRiskRowDto(
    Guid MerchantId,
    string MerchantName,
    string? Category,
    decimal RiskScore,
    string RiskLabel,
    int TotalCases,
    int AnalyzedCases,
    int LikelyCardSurchargeCases);

public sealed record AdminBranchRiskRowDto(
    Guid BranchId,
    Guid MerchantId,
    string BranchName,
    string MerchantName,
    string City,
    string Province,
    decimal RiskScore,
    string RiskLabel,
    int TotalCases,
    int AnalyzedCases,
    int LikelyCardSurchargeCases);

public sealed record RecentUploadDto(
    string UploadKind,
    Guid CaseId,
    Guid MerchantId,
    string MerchantName,
    Guid? BranchId,
    string? BranchName,
    string? City,
    string? Province,
    string FileName,
    string EvidenceType,
    string StoragePath,
    string UploadedBy,
    DateTimeOffset UploadedUtc);

public sealed record AdminDashboardSummaryDto(
    int TotalCases,
    int UnresolvedCases,
    int ComplaintPackGenerationCount,
    decimal OcrSuccessRate,
    int OcrAttemptCount,
    int OcrSuccessCount,
    IReadOnlyCollection<ClassificationCountDto> CasesByClassification,
    IReadOnlyCollection<AdminMerchantRiskRowDto> TopMerchants,
    IReadOnlyCollection<AdminBranchRiskRowDto> TopBranches);

public sealed record AdminDashboardCsvExportDto(
    string FileName,
    string ContentType,
    byte[] Content);
