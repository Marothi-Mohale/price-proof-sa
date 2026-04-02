namespace PriceProof.Application.Abstractions.Diagnostics;

public interface IAuditLogWriter
{
    void Write(
        string entityName,
        string action,
        object? payload,
        DateTimeOffset occurredAtUtc,
        Guid? actorUserId = null,
        Guid? caseId = null);

    Task WriteAndSaveAsync(
        string entityName,
        string action,
        object? payload,
        DateTimeOffset occurredAtUtc,
        Guid? actorUserId = null,
        Guid? caseId = null,
        CancellationToken cancellationToken = default);
}
