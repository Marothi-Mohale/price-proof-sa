namespace PriceProof.Infrastructure.Options;

public sealed class FileUploadOptions
{
    public const string SectionName = "Uploads";

    public string StorageRootPath { get; set; } = "storage";

    public long MaxFileSizeBytes { get; set; } = 20 * 1024 * 1024;

    public int MaxFileNameLength { get; set; } = 120;

    public int MaxCategoryLength { get; set; } = 48;

    public string[] AllowedExtensions { get; set; } =
    [
        ".jpg",
        ".jpeg",
        ".png",
        ".webp",
        ".pdf",
        ".txt",
        ".mp3",
        ".m4a",
        ".wav",
        ".mp4",
        ".webm"
    ];

    public string[] AllowedContentTypes { get; set; } =
    [
        "image/jpeg",
        "image/png",
        "image/webp",
        "application/pdf",
        "text/plain",
        "audio/mpeg",
        "audio/mp4",
        "audio/wav",
        "video/mp4",
        "video/webm"
    ];
}
