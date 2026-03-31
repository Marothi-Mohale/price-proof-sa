namespace PriceProof.Application.PriceCaptures;

public sealed record PriceCaptureDto(
    Guid Id,
    Guid CaseId,
    Guid CapturedByUserId,
    string CaptureType,
    string EvidenceType,
    decimal? QuotedAmount,
    string CurrencyCode,
    string FileName,
    string? ContentType,
    string EvidenceStoragePath,
    string? EvidenceHash,
    string? MerchantStatement,
    string? Notes,
    DateTimeOffset CapturedAtUtc,
    DateTimeOffset CreatedUtc,
    string CaseClassification,
    decimal? DifferenceAmount);
