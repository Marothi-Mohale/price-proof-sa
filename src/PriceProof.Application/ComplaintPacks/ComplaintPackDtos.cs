namespace PriceProof.Application.ComplaintPacks;

public sealed record ComplaintPackLocationDto(
    string MerchantName,
    string? MerchantCategory,
    string? MerchantWebsiteUrl,
    string? BranchName,
    string? BranchCode,
    string? AddressLine1,
    string? AddressLine2,
    string? City,
    string? Province,
    string? PostalCode);

public sealed record ComplaintPackAmountsDto(
    string CurrencyCode,
    decimal QuotedAmount,
    decimal ChargedAmount,
    decimal DiscrepancyAmount,
    decimal? PercentageDifference);

public sealed record ComplaintPackAnalysisDto(
    string Classification,
    string ClassificationLabel,
    decimal? Confidence,
    string Explanation);

public sealed record ComplaintPackEvidenceAssessmentDto(
    string Strength,
    string Explanation);

public sealed record ComplaintPackTimelineItemDto(
    DateTimeOffset OccurredAtUtc,
    string Title,
    string Description);

public sealed record ComplaintPackEvidenceItemDto(
    string Category,
    string Label,
    string FileName,
    string? ContentType,
    string StoragePath,
    string? ReferenceLink,
    DateTimeOffset RecordedAtUtc,
    string CurrencyCode,
    decimal? Amount,
    string? Notes);

public sealed record ComplaintPackSubmissionRouteDto(
    int Order,
    string Channel,
    string Recipient,
    string Reason,
    string WhenToUse);

public sealed record ComplaintPackEmailTemplateDto(
    string Subject,
    string Body);

public sealed record ComplaintPackSubmissionGuidanceDto(
    IReadOnlyCollection<ComplaintPackSubmissionRouteDto> RecommendedRoutes,
    string SafeUseNote,
    ComplaintPackEmailTemplateDto EmailTemplate);

public sealed record ComplaintPackJsonSummaryDto(
    Guid CaseId,
    string CaseReferenceNumber,
    ComplaintPackLocationDto Location,
    ComplaintPackAmountsDto Amounts,
    ComplaintPackAnalysisDto Analysis,
    ComplaintPackEvidenceAssessmentDto EvidenceAssessment,
    IReadOnlyCollection<ComplaintPackTimelineItemDto> Timeline,
    IReadOnlyCollection<ComplaintPackEvidenceItemDto> EvidenceInventory,
    ComplaintPackSubmissionGuidanceDto SubmissionGuidance,
    string ComplaintSummary,
    string DeclarationText,
    DateTimeOffset AuditTimestampUtc);

public sealed record GeneratedComplaintPackDto(
    Guid Id,
    Guid CaseId,
    string CaseReferenceNumber,
    string FileName,
    string ContentType,
    string DownloadUrl,
    string ContentHash,
    long FileSizeBytes,
    string Summary,
    ComplaintPackJsonSummaryDto JsonSummary,
    DateTimeOffset GeneratedAtUtc);

public sealed record ComplaintPackDownloadDto(
    string FileName,
    string ContentType,
    byte[] Content);
