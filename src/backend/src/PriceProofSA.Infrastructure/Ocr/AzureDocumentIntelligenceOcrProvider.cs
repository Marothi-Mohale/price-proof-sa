using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.Extensions.Options;
using PriceProofSA.Application.Abstractions.Ocr;
using PriceProofSA.Infrastructure.Options;

namespace PriceProofSA.Infrastructure.Ocr;

public sealed class AzureDocumentIntelligenceOcrProvider : IOcrProvider
{
    private readonly HttpClient _httpClient;
    private readonly OcrOptions.AzureDocumentIntelligenceOptions _options;

    public AzureDocumentIntelligenceOcrProvider(HttpClient httpClient, IOptions<OcrOptions> options)
    {
        _httpClient = httpClient;
        _options = options.Value.AzureDocumentIntelligence;
    }

    public string Name => "AzureDocumentIntelligence";

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(_options.Endpoint) &&
        !string.IsNullOrWhiteSpace(_options.ApiKey);

    public async Task<OcrProviderResult> TryRecognizeAsync(OcrDocumentRequest request, CancellationToken cancellationToken = default)
    {
        if (!IsConfigured)
        {
            return new OcrProviderResult(false, string.Empty, null, "Azure Document Intelligence is not configured.");
        }

        using var message = new HttpRequestMessage(
            HttpMethod.Post,
            $"{_options.Endpoint!.TrimEnd('/')}/documentintelligence/documentModels/{_options.ModelId}:analyze?api-version={_options.ApiVersion}");

        message.Headers.Add("Ocp-Apim-Subscription-Key", _options.ApiKey);
        message.Content = new ByteArrayContent(request.Content);
        message.Content.Headers.ContentType = MediaTypeHeaderValue.Parse(request.ContentType);

        using var response = await _httpClient.SendAsync(message, cancellationToken);
        if (!response.IsSuccessStatusCode && response.StatusCode != System.Net.HttpStatusCode.Accepted)
        {
            return new OcrProviderResult(false, string.Empty, null, $"Azure Document Intelligence request failed with status {(int)response.StatusCode}.");
        }

        var operationLocation = response.Headers.TryGetValues("operation-location", out var values)
            ? values.FirstOrDefault()
            : null;

        if (string.IsNullOrWhiteSpace(operationLocation))
        {
            return new OcrProviderResult(false, string.Empty, null, "Azure Document Intelligence did not return an operation location.");
        }

        for (var attempt = 0; attempt < 10; attempt++)
        {
            await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
            using var pollResponse = await _httpClient.GetAsync(operationLocation, cancellationToken);
            if (!pollResponse.IsSuccessStatusCode)
            {
                continue;
            }

            await using var stream = await pollResponse.Content.ReadAsStreamAsync(cancellationToken);
            using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
            var status = document.RootElement.GetProperty("status").GetString();
            if (status?.Equals("succeeded", StringComparison.OrdinalIgnoreCase) == true)
            {
                var rawText = document.RootElement
                    .GetProperty("analyzeResult")
                    .TryGetProperty("content", out var contentElement)
                    ? contentElement.GetString() ?? string.Empty
                    : string.Empty;

                return new OcrProviderResult(!string.IsNullOrWhiteSpace(rawText), rawText, null, "Azure Document Intelligence OCR completed.");
            }

            if (status?.Equals("failed", StringComparison.OrdinalIgnoreCase) == true)
            {
                return new OcrProviderResult(false, string.Empty, null, "Azure Document Intelligence OCR failed.");
            }
        }

        return new OcrProviderResult(false, string.Empty, null, "Azure Document Intelligence timed out while waiting for analysis completion.");
    }
}
