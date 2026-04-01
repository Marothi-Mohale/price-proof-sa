namespace PriceProof.Application.Abstractions.Ocr;

public interface IOcrOrchestrator
{
    Task<OcrReceiptResult> RecognizeReceiptAsync(OcrDocumentContent document, CancellationToken cancellationToken = default);
}
