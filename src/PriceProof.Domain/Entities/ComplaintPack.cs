using PriceProof.Domain.Common;

namespace PriceProof.Domain.Entities;

public sealed class ComplaintPack : SoftDeletableEntity
{
    private ComplaintPack()
    {
    }

    public Guid CaseId { get; private set; }

    public DiscrepancyCase? Case { get; private set; }

    public Guid? GeneratedByUserId { get; private set; }

    public User? GeneratedByUser { get; private set; }

    public string FileName { get; private set; } = string.Empty;

    public string StoragePath { get; private set; } = string.Empty;

    public string ContentHash { get; private set; } = string.Empty;

    public string Summary { get; private set; } = string.Empty;

    public DateTimeOffset GeneratedAtUtc { get; private set; }

    public static ComplaintPack Create(
        Guid caseId,
        Guid? generatedByUserId,
        string fileName,
        string storagePath,
        string contentHash,
        string summary,
        DateTimeOffset generatedAtUtc)
    {
        return new ComplaintPack
        {
            CaseId = caseId,
            GeneratedByUserId = generatedByUserId,
            FileName = fileName.Trim(),
            StoragePath = storagePath.Trim(),
            ContentHash = contentHash.Trim(),
            Summary = summary.Trim(),
            GeneratedAtUtc = generatedAtUtc
        };
    }
}
