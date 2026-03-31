namespace PriceProofSA.Application.Abstractions.Ocr;

public interface IOcrOrchestrator
{
    Task<OcrDocumentResult> RecognizeReceiptAsync(OcrDocumentRequest request, CancellationToken cancellationToken = default);
}
