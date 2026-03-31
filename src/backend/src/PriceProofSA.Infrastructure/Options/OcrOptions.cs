namespace PriceProofSA.Infrastructure.Options;

public sealed class OcrOptions
{
    public const string SectionName = "Ocr";

    public AzureDocumentIntelligenceOptions AzureDocumentIntelligence { get; set; } = new();

    public GoogleVisionOptions GoogleVision { get; set; } = new();

    public sealed class AzureDocumentIntelligenceOptions
    {
        public string? Endpoint { get; set; }

        public string? ApiKey { get; set; }

        public string ModelId { get; set; } = "prebuilt-read";

        public string ApiVersion { get; set; } = "2024-02-29-preview";
    }

    public sealed class GoogleVisionOptions
    {
        public string? ApiKey { get; set; }
    }
}
