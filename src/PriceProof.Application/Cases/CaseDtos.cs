namespace PriceProof.Application.Cases;

public sealed record UserReferenceDto(Guid Id, string DisplayName, string Email);

public sealed record MerchantReferenceDto(Guid Id, string Name, string? Category, string? WebsiteUrl);

public sealed record BranchReferenceDto(
    Guid Id,
    Guid MerchantId,
    string Name,
    string? Code,
    string AddressLine1,
    string? AddressLine2,
    string City,
    string Province,
    string? PostalCode);

public sealed record PriceCaptureSummaryDto(
    Guid Id,
    string CaptureType,
    string EvidenceType,
    decimal? QuotedAmount,
    string CurrencyCode,
    string FileName,
    string? ContentType,
    string EvidenceStoragePath,
    string? MerchantStatement,
    string? Notes,
    DateTimeOffset CapturedAtUtc,
    DateTimeOffset CreatedUtc);

public sealed record ReceiptSummaryDto(
    Guid Id,
    string EvidenceType,
    string FileName,
    string ContentType,
    string StoragePath,
    string CurrencyCode,
    decimal? ParsedTotalAmount,
    string? ReceiptNumber,
    string? MerchantName,
    string? RawText,
    DateTimeOffset UploadedAtUtc,
    DateTimeOffset CreatedUtc);

public sealed record PaymentRecordSummaryDto(
    Guid Id,
    string PaymentMethod,
    decimal Amount,
    string CurrencyCode,
    string? PaymentReference,
    string? MerchantReference,
    string? CardLastFour,
    string? Notes,
    DateTimeOffset PaidAtUtc,
    DateTimeOffset CreatedUtc,
    ReceiptSummaryDto? Receipt);

public sealed record ComplaintPackDto(
    Guid Id,
    string FileName,
    string StoragePath,
    string ContentHash,
    string Summary,
    string DownloadUrl,
    DateTimeOffset GeneratedAtUtc);

public sealed record AuditLogDto(
    Guid Id,
    string EntityName,
    string Action,
    string CorrelationId,
    DateTimeOffset OccurredAtUtc,
    DateTimeOffset CreatedUtc);

public sealed record CaseAnalysisDto(
    decimal QuotedAmount,
    decimal ChargedAmount,
    decimal Difference,
    decimal? PercentageDifference,
    string Classification,
    decimal Confidence,
    string Explanation);

public sealed record CaseSummaryDto(
    Guid Id,
    string CaseNumber,
    MerchantReferenceDto Merchant,
    BranchReferenceDto? Branch,
    string BasketDescription,
    string Status,
    string Classification,
    decimal? LatestQuotedAmount,
    decimal? LatestPaidAmount,
    decimal? DifferenceAmount,
    DateTimeOffset IncidentAtUtc,
    DateTimeOffset CreatedUtc,
    DateTimeOffset UpdatedUtc);

public sealed record CaseDetailDto(
    Guid Id,
    string CaseNumber,
    UserReferenceDto ReportedBy,
    MerchantReferenceDto Merchant,
    BranchReferenceDto? Branch,
    string BasketDescription,
    string CurrencyCode,
    string Status,
    string Classification,
    decimal? LatestQuotedAmount,
    decimal? LatestPaidAmount,
    decimal? DifferenceAmount,
    DateTimeOffset IncidentAtUtc,
    string? CustomerReference,
    string? Notes,
    DateTimeOffset CreatedUtc,
    DateTimeOffset UpdatedUtc,
    IReadOnlyCollection<PriceCaptureSummaryDto> PriceCaptures,
    IReadOnlyCollection<PaymentRecordSummaryDto> PaymentRecords,
    IReadOnlyCollection<ComplaintPackDto> ComplaintPacks,
    IReadOnlyCollection<AuditLogDto> AuditLogs);
