namespace PriceProof.Infrastructure.Persistence.Entities;

public sealed class StoredBinaryObject
{
    public Guid Id { get; private set; }

    public string Bucket { get; private set; } = string.Empty;

    public string StorageKey { get; private set; } = string.Empty;

    public string FileName { get; private set; } = string.Empty;

    public string ContentType { get; private set; } = string.Empty;

    public string ContentHash { get; private set; } = string.Empty;

    public long SizeBytes { get; private set; }

    public byte[] Content { get; private set; } = [];

    public Guid? CaseId { get; private set; }

    public DateTimeOffset CreatedUtc { get; private set; }

    public DateTimeOffset UpdatedUtc { get; private set; }

    public static StoredBinaryObject Create(
        string bucket,
        string storageKey,
        string fileName,
        string contentType,
        string contentHash,
        byte[] content,
        Guid? caseId,
        DateTimeOffset now)
    {
        return new StoredBinaryObject
        {
            Id = Guid.NewGuid(),
            Bucket = bucket.Trim(),
            StorageKey = storageKey.Trim(),
            FileName = fileName.Trim(),
            ContentType = contentType.Trim(),
            ContentHash = contentHash.Trim(),
            Content = content,
            SizeBytes = content.LongLength,
            CaseId = caseId,
            CreatedUtc = now,
            UpdatedUtc = now
        };
    }
}
