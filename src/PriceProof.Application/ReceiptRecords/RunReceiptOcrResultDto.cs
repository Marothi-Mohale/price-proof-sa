namespace PriceProof.Application.ReceiptRecords;

public sealed record ReceiptOcrLineItemDto(
    string Description,
    decimal? TotalAmount,
    decimal? Quantity,
    decimal? UnitPrice);

public sealed record RunReceiptOcrResultDto(
    Guid ReceiptRecordId,
    string ProviderName,
    decimal Confidence,
    string RawPayloadMetadataJson,
    string? MerchantName,
    decimal? TransactionTotal,
    DateTimeOffset? TransactionAtUtc,
    IReadOnlyCollection<ReceiptOcrLineItemDto> LineItems,
    string? RawText,
    DateTimeOffset ProcessedAtUtc);
