using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Options;
using PriceProofSA.Application.Abstractions.Ocr;
using PriceProofSA.Infrastructure.Options;

namespace PriceProofSA.Infrastructure.Ocr;

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

    public async Task<OcrProviderResult> TryRecognizeAsync(OcrDocumentRequest request, CancellationToken cancellationToken = default)
    {
        if (!IsConfigured)
        {
            return new OcrProviderResult(false, string.Empty, null, "Google Vision is not configured.");
        }

        var response = await _httpClient.PostAsJsonAsync(
            $"https://vision.googleapis.com/v1/images:annotate?key={_options.ApiKey}",
            new
            {
                requests = new[]
                {
                    new
                    {
                        image = new { content = Convert.ToBase64String(request.Content) },
                        features = new[] { new { type = "TEXT_DETECTION" } }
                    }
                }
            },
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            return new OcrProviderResult(false, string.Empty, null, $"Google Vision request failed with status {(int)response.StatusCode}.");
        }

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
        var rawText = document.RootElement
            .GetProperty("responses")[0]
            .TryGetProperty("fullTextAnnotation", out var fullTextAnnotation) &&
                      fullTextAnnotation.TryGetProperty("text", out var textElement)
            ? textElement.GetString() ?? string.Empty
            : string.Empty;

        return new OcrProviderResult(!string.IsNullOrWhiteSpace(rawText), rawText, null, "Google Vision OCR completed.");
    }
}
