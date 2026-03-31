namespace PriceProofSA.Infrastructure.Options;

public sealed class StorageOptions
{
    public const string SectionName = "Storage";

    public string Provider { get; set; } = "Local";

    public string LocalRootPath { get; set; } = "storage";

    public string? AzureBlobConnectionString { get; set; }

    public string AzureContainerPrefix { get; set; } = "priceproof";
}
