namespace PriceProof.Application.ReceiptRecords;

public sealed record ReceiptRecordDto(
    Guid Id,
    Guid CaseId,
    Guid PaymentRecordId,
    Guid UploadedByUserId,
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
