namespace PriceProofSA.Application.Abstractions.Audit;

public sealed record AuditEntryInput(
    Guid? UserId,
    Guid? CaseId,
    string EntityType,
    Guid EntityId,
    string Action,
    object Payload);
