namespace PriceProof.Infrastructure.Options;

public sealed class FileUploadOptions
{
    public const string SectionName = "Uploads";

    public string StorageRootPath { get; set; } = "storage";

    public long MaxFileSizeBytes { get; set; } = 20 * 1024 * 1024;
}
