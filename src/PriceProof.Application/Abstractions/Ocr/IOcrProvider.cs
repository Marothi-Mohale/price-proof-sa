namespace PriceProof.Application.Abstractions.Ocr;

public interface IOcrProvider
{
    string Name { get; }

    bool IsConfigured { get; }

    Task<OcrProviderResult> RecognizeAsync(OcrDocumentContent document, CancellationToken cancellationToken = default);
}
