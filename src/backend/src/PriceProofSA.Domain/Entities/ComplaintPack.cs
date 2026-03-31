using PriceProofSA.Domain.Common;

namespace PriceProofSA.Domain.Entities;

public sealed class ComplaintPack : BaseEntity
{
    private ComplaintPack()
    {
    }

    public Guid CaseId { get; private set; }

    public DiscrepancyCase? Case { get; private set; }

    public string FileName { get; private set; } = string.Empty;

    public string StoragePath { get; private set; } = string.Empty;

    public string Summary { get; private set; } = string.Empty;

    public string FileHash { get; private set; } = string.Empty;

    public DateTimeOffset GeneratedAtUtc { get; private set; }

    public static ComplaintPack Create(
        Guid caseId,
        string fileName,
        string storagePath,
        string summary,
        string fileHash,
        DateTimeOffset generatedAtUtc)
    {
        return new ComplaintPack
        {
            CaseId = caseId,
            FileName = fileName,
            StoragePath = storagePath,
            Summary = summary,
            FileHash = fileHash,
            GeneratedAtUtc = generatedAtUtc
        };
    }
}
