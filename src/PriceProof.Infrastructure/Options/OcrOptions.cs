namespace PriceProof.Infrastructure.Options;

public sealed class OcrOptions
{
    public const string SectionName = "Ocr";

    public bool Enabled { get; set; }

    public string PrimaryProvider { get; set; } = "AzureDocumentIntelligence";

    public string? SecondaryProvider { get; set; } = "GoogleVision";

    public int RetryCount { get; set; } = 1;

    public int RequestTimeoutSeconds { get; set; } = 20;

    public int ProviderRetryCount { get; set; } = 2;

    public int ProviderRetryDelayMilliseconds { get; set; } = 500;

    public string StorageRootPath { get; set; } = "storage";

    public AzureDocumentIntelligenceOptions AzureDocumentIntelligence { get; set; } = new();

    public GoogleVisionOptions GoogleVision { get; set; } = new();

    public sealed class AzureDocumentIntelligenceOptions
    {
        public string? Endpoint { get; set; }

        public string? ApiKey { get; set; }

        public string ModelId { get; set; } = "prebuilt-receipt";

        public string ApiVersion { get; set; } = "2024-11-30";

        public int PollingIntervalMilliseconds { get; set; } = 1000;

        public int MaxPollingAttempts { get; set; } = 20;
    }

    public sealed class GoogleVisionOptions
    {
        public string? ApiKey { get; set; }
    }
}
