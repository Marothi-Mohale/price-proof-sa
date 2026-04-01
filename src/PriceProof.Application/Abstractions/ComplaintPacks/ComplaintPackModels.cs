namespace PriceProof.Application.Abstractions.ComplaintPacks;

public sealed record ComplaintPackTimelineEntry(
    DateTimeOffset OccurredAtUtc,
    string Title,
    string Description);

public sealed record ComplaintPackEvidenceEntry(
    string Category,
    string Label,
    string FileName,
    string? ContentType,
    string StoragePath,
    DateTimeOffset RecordedAtUtc,
    string? ReferenceLink,
    string CurrencyCode,
    decimal? Amount,
    string? Notes);

public sealed record ComplaintPackBuildRequest(
    string CaseReferenceNumber,
    string MerchantName,
    string? MerchantCategory,
    string? MerchantWebsiteUrl,
    string? BranchName,
    string? BranchCode,
    string? BranchAddressLine1,
    string? BranchAddressLine2,
    string? BranchCity,
    string? BranchProvince,
    string? BranchPostalCode,
    string ReportedByDisplayName,
    string ReportedByEmail,
    string BasketDescription,
    DateTimeOffset IncidentAtUtc,
    string CurrencyCode,
    decimal QuotedAmount,
    decimal ChargedAmount,
    decimal DifferenceAmount,
    decimal? PercentageDifference,
    string Classification,
    string ClassificationLabel,
    decimal? Confidence,
    string Explanation,
    string EvidenceStrength,
    string EvidenceStrengthExplanation,
    IReadOnlyCollection<ComplaintPackTimelineEntry> Timeline,
    IReadOnlyCollection<ComplaintPackEvidenceEntry> EvidenceInventory,
    string ComplaintSummary,
    string DeclarationText,
    DateTimeOffset AuditTimestampUtc);

public sealed record GeneratedComplaintPackDocument(
    string FileName,
    string ContentType,
    byte[] Content);

public sealed record StoredComplaintPackDocument(
    string FileName,
    string StoragePath,
    string ContentHash,
    long SizeBytes);

public sealed record ComplaintPackDownloadFile(
    string FileName,
    string ContentType,
    byte[] Content);
