namespace PriceProof.Application.Abstractions.Ocr;

public sealed record OcrDocumentContent(
    string FileName,
    string ContentType,
    byte[] Content);

public sealed record OcrLineItem(
    string Description,
    decimal? TotalAmount,
    decimal? Quantity = null,
    decimal? UnitPrice = null);

public sealed record OcrProviderResult(
    string ProviderName,
    bool Success,
    string RawText,
    string RawPayloadMetadataJson,
    decimal? Confidence = null,
    string? MerchantName = null,
    decimal? TransactionTotal = null,
    DateTimeOffset? TransactionAtUtc = null,
    IReadOnlyCollection<OcrLineItem>? LineItems = null,
    string? ReceiptNumber = null,
    string? FailureMessage = null,
    bool IsTransientFailure = true);

public sealed record OcrReceiptResult(
    string ProviderName,
    string RawText,
    string RawPayloadMetadataJson,
    decimal Confidence,
    string? MerchantName,
    decimal? TransactionTotal,
    DateTimeOffset? TransactionAtUtc,
    IReadOnlyCollection<OcrLineItem> LineItems,
    string? ReceiptNumber);
