using System.Text;
using System.Text.Json;
using PriceProof.Application.Abstractions.Ocr;

namespace PriceProof.IntegrationTests.Fakes;

public sealed class FakeOcrProvider : IOcrProvider
{
    public string Name => "AzureDocumentIntelligence";

    public bool IsConfigured => true;

    public Task<OcrProviderResult> RecognizeAsync(OcrDocumentContent document, CancellationToken cancellationToken = default)
    {
        var rawText = Encoding.UTF8.GetString(document.Content);

        var result = new OcrProviderResult(
            Name,
            true,
            rawText,
            JsonSerializer.Serialize(new
            {
                provider = Name,
                fake = true,
                fileName = document.FileName,
                contentType = document.ContentType
            }));

        return Task.FromResult(result);
    }
}
