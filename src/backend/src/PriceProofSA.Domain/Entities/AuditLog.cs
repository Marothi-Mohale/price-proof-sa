using PriceProofSA.Domain.Common;

namespace PriceProofSA.Domain.Entities;

public sealed class AuditLog : BaseEntity
{
    private AuditLog()
    {
    }

    public Guid? UserId { get; private set; }

    public Guid? CaseId { get; private set; }

    public string EntityType { get; private set; } = string.Empty;

    public Guid EntityId { get; private set; }

    public string Action { get; private set; } = string.Empty;

    public string PayloadJson { get; private set; } = string.Empty;

    public string Hash { get; private set; } = string.Empty;

    public string? PreviousHash { get; private set; }

    public DateTimeOffset OccurredAtUtc { get; private set; }

    public static AuditLog Create(
        Guid? userId,
        Guid? caseId,
        string entityType,
        Guid entityId,
        string action,
        string payloadJson,
        string hash,
        string? previousHash,
        DateTimeOffset occurredAtUtc)
    {
        return new AuditLog
        {
            UserId = userId,
            CaseId = caseId,
            EntityType = entityType,
            EntityId = entityId,
            Action = action,
            PayloadJson = payloadJson,
            Hash = hash,
            PreviousHash = previousHash,
            OccurredAtUtc = occurredAtUtc
        };
    }
}
