namespace PriceProof.Application.Abstractions.Ocr;

public interface IReceiptDocumentContentResolver
{
    Task<OcrDocumentContent> ResolveAsync(
        string fileName,
        string contentType,
        string storagePath,
        CancellationToken cancellationToken = default);
}
