namespace PriceProof.Api.Options;

public sealed class OpenTelemetryOptions
{
    public const string SectionName = "OpenTelemetry";

    public bool Enabled { get; set; }

    public string ServiceName { get; set; } = "PriceProof.Api";

    public string? ServiceVersion { get; set; }

    public string? OtlpEndpoint { get; set; }

    public bool EnableConsoleExporter { get; set; }
}
