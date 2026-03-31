using PriceProofSA.Domain.Common;
using PriceProofSA.Domain.Enums;

namespace PriceProofSA.Domain.Entities;

public sealed class PriceEvidence : BaseEntity
{
    private PriceEvidence()
    {
    }

    public Guid PriceCaptureId { get; private set; }

    public PriceCapture? PriceCapture { get; private set; }

    public EvidenceFileType FileType { get; private set; }

    public string FileName { get; private set; } = string.Empty;

    public string ContentType { get; private set; } = string.Empty;

    public string StoragePath { get; private set; } = string.Empty;

    public long SizeBytes { get; private set; }

    public string ContentHash { get; private set; } = string.Empty;

    public DateTimeOffset UploadedAtUtc { get; private set; }

    public static PriceEvidence Create(
        Guid priceCaptureId,
        EvidenceFileType fileType,
        string fileName,
        string contentType,
        string storagePath,
        long sizeBytes,
        string contentHash,
        DateTimeOffset uploadedAtUtc)
    {
        return new PriceEvidence
        {
            PriceCaptureId = priceCaptureId,
            FileType = fileType,
            FileName = fileName,
            ContentType = contentType,
            StoragePath = storagePath,
            SizeBytes = sizeBytes,
            ContentHash = contentHash,
            UploadedAtUtc = uploadedAtUtc
        };
    }
}
