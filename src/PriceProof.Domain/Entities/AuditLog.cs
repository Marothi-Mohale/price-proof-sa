using PriceProof.Domain.Common;

namespace PriceProof.Domain.Entities;

public sealed class AuditLog : AuditableEntity
{
    private AuditLog()
    {
    }

    public Guid? CaseId { get; private set; }

    public DiscrepancyCase? Case { get; private set; }

    public Guid? ActorUserId { get; private set; }

    public User? ActorUser { get; private set; }

    public string EntityName { get; private set; } = string.Empty;

    public string Action { get; private set; } = string.Empty;

    public string PayloadJson { get; private set; } = string.Empty;

    public string CorrelationId { get; private set; } = string.Empty;

    public DateTimeOffset OccurredAtUtc { get; private set; }

    public static AuditLog Create(
        string entityName,
        string action,
        string payloadJson,
        string correlationId,
        DateTimeOffset occurredAtUtc,
        Guid? actorUserId = null,
        Guid? caseId = null)
    {
        return new AuditLog
        {
            EntityName = entityName.Trim(),
            Action = action.Trim(),
            PayloadJson = payloadJson,
            CorrelationId = correlationId.Trim(),
            OccurredAtUtc = occurredAtUtc,
            ActorUserId = actorUserId,
            CaseId = caseId
        };
    }
}
