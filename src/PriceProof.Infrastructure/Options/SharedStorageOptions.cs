namespace PriceProof.Infrastructure.Options;

public sealed class SharedStorageOptions
{
    public const string SectionName = "SharedStorage";

    public string StorageKeyPrefix { get; set; } = "db://";

    public bool EnableLegacyFileFallback { get; set; } = true;

    public string LegacyFileRootPath { get; set; } = "storage";
}
