namespace PriceProofSA.Application.Abstractions.Ocr;

public interface IOcrProvider
{
    string Name { get; }

    bool IsConfigured { get; }

    Task<OcrProviderResult> TryRecognizeAsync(OcrDocumentRequest request, CancellationToken cancellationToken = default);
}
