namespace PriceProofSA.Application.Abstractions.Ocr;

public sealed record OcrDocumentRequest(
    string FileName,
    string ContentType,
    byte[] Content);

public sealed record OcrProviderResult(
    bool Success,
    string RawText,
    decimal? ParsedAmount,
    string Message);

public sealed record OcrDocumentResult(
    bool Success,
    string ProviderName,
    string RawText,
    decimal? ParsedAmount,
    string Message,
    bool NoProviderConfigured = false);
