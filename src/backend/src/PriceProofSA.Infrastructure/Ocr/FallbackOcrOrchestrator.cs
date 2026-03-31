using Microsoft.Extensions.Logging;
using PriceProofSA.Application.Abstractions.Ocr;

namespace PriceProofSA.Infrastructure.Ocr;

public sealed class FallbackOcrOrchestrator : IOcrOrchestrator
{
    private readonly IReadOnlyCollection<IOcrProvider> _providers;
    private readonly ReceiptTotalParser _receiptTotalParser;
    private readonly ILogger<FallbackOcrOrchestrator> _logger;

    public FallbackOcrOrchestrator(
        IEnumerable<IOcrProvider> providers,
        ReceiptTotalParser receiptTotalParser,
        ILogger<FallbackOcrOrchestrator> logger)
    {
        _providers = providers.ToArray();
        _receiptTotalParser = receiptTotalParser;
        _logger = logger;
    }

    public async Task<OcrDocumentResult> RecognizeReceiptAsync(OcrDocumentRequest request, CancellationToken cancellationToken = default)
    {
        var configuredProviders = _providers.Where(provider => provider.IsConfigured).ToArray();
        if (configuredProviders.Length == 0)
        {
            return new OcrDocumentResult(false, "None", string.Empty, null, "No OCR provider is configured.", true);
        }

        foreach (var provider in configuredProviders)
        {
            try
            {
                var result = await provider.TryRecognizeAsync(request, cancellationToken);
                if (!result.Success && string.IsNullOrWhiteSpace(result.RawText))
                {
                    _logger.LogWarning("OCR provider {Provider} failed: {Message}", provider.Name, result.Message);
                    continue;
                }

                var parsedAmount = result.ParsedAmount ?? _receiptTotalParser.Parse(result.RawText);
                return new OcrDocumentResult(result.Success, provider.Name, result.RawText, parsedAmount, result.Message);
            }
            catch (Exception exception)
            {
                _logger.LogWarning(exception, "OCR provider {Provider} threw an exception and the orchestrator will try the next provider.", provider.Name);
            }
        }

        return new OcrDocumentResult(false, "Fallback", string.Empty, null, "OCR could not parse the receipt.");
    }
}
