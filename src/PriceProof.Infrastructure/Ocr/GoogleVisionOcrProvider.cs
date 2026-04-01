using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Options;
using PriceProof.Application.Abstractions.Ocr;
using PriceProof.Infrastructure.Options;

namespace PriceProof.Infrastructure.Ocr;

public sealed class GoogleVisionOcrProvider : IOcrProvider
{
    private readonly HttpClient _httpClient;
    private readonly OcrOptions.GoogleVisionOptions _options;

    public GoogleVisionOcrProvider(HttpClient httpClient, IOptions<OcrOptions> options)
    {
        _httpClient = httpClient;
        _options = options.Value.GoogleVision;
    }

    public string Name => "GoogleVision";

    public bool IsConfigured => !string.IsNullOrWhiteSpace(_options.ApiKey);

    public async Task<OcrProviderResult> RecognizeAsync(OcrDocumentContent document, CancellationToken cancellationToken = default)
    {
        if (!IsConfigured)
        {
            return Failure("Google Vision is not configured.", false);
        }

        using var response = await _httpClient.PostAsJsonAsync(
            $"https://vision.googleapis.com/v1/images:annotate?key={_options.ApiKey}",
            new
            {
                requests = new[]
                {
                    new
                    {
                        image = new { content = Convert.ToBase64String(document.Content) },
                        features = new[] { new { type = "DOCUMENT_TEXT_DETECTION" } }
                    }
                }
            },
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            return Failure($"Google Vision request failed with status {(int)response.StatusCode}.", IsTransientStatus(response.StatusCode));
        }

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var documentJson = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);

        if (!documentJson.RootElement.TryGetProperty("responses", out var responses) ||
            responses.ValueKind != JsonValueKind.Array ||
            responses.GetArrayLength() == 0)
        {
            return Failure("Google Vision returned an empty response.", true);
        }

        var responseElement = responses[0];
        if (responseElement.TryGetProperty("error", out var error) &&
            error.TryGetProperty("message", out var errorMessage))
        {
            return Failure($"Google Vision OCR failed: {errorMessage.GetString()}", false);
        }

        var rawText = responseElement.TryGetProperty("fullTextAnnotation", out var fullText) &&
                      fullText.TryGetProperty("text", out var textElement) &&
                      textElement.ValueKind == JsonValueKind.String
            ? textElement.GetString() ?? string.Empty
            : string.Empty;

        var confidence = GetAverageBlockConfidence(fullText);
        var metadataJson = JsonSerializer.Serialize(new
        {
            provider = Name,
            responseCount = responses.GetArrayLength(),
            pageCount = fullText.ValueKind == JsonValueKind.Object && fullText.TryGetProperty("pages", out var pages)
                ? pages.GetArrayLength()
                : 0,
            hasFullText = !string.IsNullOrWhiteSpace(rawText)
        });

        return new OcrProviderResult(
            Name,
            !string.IsNullOrWhiteSpace(rawText),
            rawText,
            metadataJson,
            confidence);
    }

    private static decimal? GetAverageBlockConfidence(JsonElement fullText)
    {
        if (fullText.ValueKind != JsonValueKind.Object ||
            !fullText.TryGetProperty("pages", out var pages) ||
            pages.ValueKind != JsonValueKind.Array)
        {
            return null;
        }

        var values = new List<decimal>();

        foreach (var page in pages.EnumerateArray())
        {
            if (!page.TryGetProperty("blocks", out var blocks) || blocks.ValueKind != JsonValueKind.Array)
            {
                continue;
            }

            foreach (var block in blocks.EnumerateArray())
            {
                if (block.TryGetProperty("confidence", out var confidence) && confidence.ValueKind == JsonValueKind.Number)
                {
                    values.Add(confidence.GetDecimal());
                }
            }
        }

        return values.Count == 0 ? null : decimal.Round(values.Average(), 4, MidpointRounding.AwayFromZero);
    }

    private static bool IsTransientStatus(HttpStatusCode statusCode)
    {
        var status = (int)statusCode;
        return status == 408 || status == 429 || status >= 500;
    }

    private static OcrProviderResult Failure(string message, bool isTransientFailure)
    {
        return new OcrProviderResult(
            "GoogleVision",
            false,
            string.Empty,
            "{}",
            null,
            null,
            null,
            null,
            null,
            null,
            message,
            isTransientFailure);
    }
}
