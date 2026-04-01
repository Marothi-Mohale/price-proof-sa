using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PriceProof.Application.Abstractions.Ocr;
using PriceProof.Application.Common.Exceptions;
using PriceProof.Infrastructure.Options;

namespace PriceProof.Infrastructure.Ocr;

public sealed class OcrOrchestrator : IOcrOrchestrator
{
    private readonly IReadOnlyDictionary<string, IOcrProvider> _providers;
    private readonly ReceiptOcrTextParser _parser;
    private readonly OcrOptions _options;
    private readonly ILogger<OcrOrchestrator> _logger;

    public OcrOrchestrator(
        IEnumerable<IOcrProvider> providers,
        ReceiptOcrTextParser parser,
        IOptions<OcrOptions> options,
        ILogger<OcrOrchestrator> logger)
    {
        _providers = providers.ToDictionary(provider => provider.Name, StringComparer.OrdinalIgnoreCase);
        _parser = parser;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<OcrReceiptResult> RecognizeReceiptAsync(OcrDocumentContent document, CancellationToken cancellationToken = default)
    {
        if (!_options.Enabled)
        {
            throw new ServiceUnavailableException("Receipt OCR is currently unavailable. Please try again later.");
        }

        var orderedProviders = ResolveProviderOrder();
        if (orderedProviders.Count == 0)
        {
            throw new ServiceUnavailableException("Receipt OCR is currently unavailable. Please try again later.");
        }

        var retryCount = Math.Max(0, _options.RetryCount);
        var timeout = TimeSpan.FromSeconds(Math.Max(1, _options.RequestTimeoutSeconds));

        foreach (var provider in orderedProviders)
        {
            for (var attempt = 1; attempt <= retryCount + 1; attempt++)
            {
                try
                {
                    using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                    timeoutCts.CancelAfter(timeout);

                    var providerResult = await provider.RecognizeAsync(document, timeoutCts.Token);
                    if (HasUsefulContent(providerResult))
                    {
                        return Normalize(providerResult);
                    }

                    _logger.LogWarning(
                        "OCR provider {Provider} returned no useful content on attempt {Attempt}. Message: {Message}",
                        provider.Name,
                        attempt,
                        providerResult.FailureMessage ?? "No failure message was provided.");

                    if (!providerResult.IsTransientFailure)
                    {
                        break;
                    }
                }
                catch (OperationCanceledException exception) when (!cancellationToken.IsCancellationRequested)
                {
                    _logger.LogWarning(
                        exception,
                        "OCR provider {Provider} timed out on attempt {Attempt} after {TimeoutSeconds} seconds.",
                        provider.Name,
                        attempt,
                        timeout.TotalSeconds);
                }
                catch (Exception exception)
                {
                    _logger.LogWarning(
                        exception,
                        "OCR provider {Provider} failed on attempt {Attempt}.",
                        provider.Name,
                        attempt);
                }
            }
        }

        throw new ServiceUnavailableException("Receipt OCR is currently unavailable. Please try again later.");
    }

    private IReadOnlyList<IOcrProvider> ResolveProviderOrder()
    {
        var names = new[] { _options.PrimaryProvider, _options.SecondaryProvider }
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var resolved = names
            .Select(name => _providers.TryGetValue(name!, out var provider) ? provider : null)
            .Where(provider => provider is not null && provider.IsConfigured)
            .Cast<IOcrProvider>()
            .ToList();

        if (resolved.Count > 0)
        {
            return resolved;
        }

        return _providers.Values.Where(provider => provider.IsConfigured).ToArray();
    }

    private OcrReceiptResult Normalize(OcrProviderResult providerResult)
    {
        var merchantName = string.IsNullOrWhiteSpace(providerResult.MerchantName)
            ? _parser.ParseMerchantName(providerResult.RawText)
            : providerResult.MerchantName.Trim();

        var transactionTotal = providerResult.TransactionTotal ?? _parser.ParseTransactionTotal(providerResult.RawText);
        var transactionAtUtc = providerResult.TransactionAtUtc ?? _parser.ParseTransactionAt(providerResult.RawText);

        var lineItems = providerResult.LineItems is { Count: > 0 }
            ? providerResult.LineItems.Where(item => !string.IsNullOrWhiteSpace(item.Description)).ToArray()
            : _parser.ParseLineItems(providerResult.RawText);

        var confidence = DetermineConfidence(providerResult, merchantName, transactionTotal, transactionAtUtc, lineItems.Count);
        var rawPayloadMetadataJson = string.IsNullOrWhiteSpace(providerResult.RawPayloadMetadataJson)
            ? "{}"
            : providerResult.RawPayloadMetadataJson;

        return new OcrReceiptResult(
            providerResult.ProviderName,
            providerResult.RawText,
            rawPayloadMetadataJson,
            confidence,
            merchantName,
            transactionTotal,
            transactionAtUtc,
            lineItems,
            providerResult.ReceiptNumber);
    }

    private static bool HasUsefulContent(OcrProviderResult providerResult)
    {
        return providerResult.Success ||
               !string.IsNullOrWhiteSpace(providerResult.RawText) ||
               !string.IsNullOrWhiteSpace(providerResult.MerchantName) ||
               providerResult.TransactionTotal.HasValue ||
               providerResult.TransactionAtUtc.HasValue ||
               providerResult.LineItems is { Count: > 0 };
    }

    private static decimal DetermineConfidence(
        OcrProviderResult providerResult,
        string? merchantName,
        decimal? transactionTotal,
        DateTimeOffset? transactionAtUtc,
        int lineItemCount)
    {
        if (providerResult.Confidence.HasValue)
        {
            return decimal.Round(Math.Clamp(providerResult.Confidence.Value, 0m, 1m), 4, MidpointRounding.AwayFromZero);
        }

        var confidence = 0.45m;

        if (!string.IsNullOrWhiteSpace(providerResult.RawText))
        {
            confidence += 0.15m;
        }

        if (!string.IsNullOrWhiteSpace(merchantName))
        {
            confidence += 0.10m;
        }

        if (transactionTotal.HasValue)
        {
            confidence += 0.15m;
        }

        if (transactionAtUtc.HasValue)
        {
            confidence += 0.05m;
        }

        if (lineItemCount > 0)
        {
            confidence += 0.10m;
        }

        return decimal.Round(Math.Clamp(confidence, 0m, 0.95m), 4, MidpointRounding.AwayFromZero);
    }
}
