using System.Text.Json;
using PriceProof.Application.Abstractions.Diagnostics;
using PriceProof.Application.Abstractions.Persistence;
using PriceProof.Domain.Entities;

namespace PriceProof.Application.Diagnostics;

internal sealed class AuditLogWriter : IAuditLogWriter
{
    private const int MaxPayloadLength = 32000;

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly IApplicationDbContext _dbContext;
    private readonly IRequestContextAccessor _requestContextAccessor;

    public AuditLogWriter(IApplicationDbContext dbContext, IRequestContextAccessor requestContextAccessor)
    {
        _dbContext = dbContext;
        _requestContextAccessor = requestContextAccessor;
    }

    public void Write(
        string entityName,
        string action,
        object? payload,
        DateTimeOffset occurredAtUtc,
        Guid? actorUserId = null,
        Guid? caseId = null)
    {
        var payloadJson = SerializePayload(payload);
        _dbContext.AuditLogs.Add(AuditLog.Create(
            entityName,
            action,
            payloadJson,
            _requestContextAccessor.CorrelationId,
            occurredAtUtc,
            actorUserId,
            caseId));
    }

    public async Task WriteAndSaveAsync(
        string entityName,
        string action,
        object? payload,
        DateTimeOffset occurredAtUtc,
        Guid? actorUserId = null,
        Guid? caseId = null,
        CancellationToken cancellationToken = default)
    {
        Write(entityName, action, payload, occurredAtUtc, actorUserId, caseId);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private static string SerializePayload(object? payload)
    {
        var payloadJson = JsonSerializer.Serialize(payload, JsonOptions);
        if (payloadJson.Length <= MaxPayloadLength)
        {
            return payloadJson;
        }

        return JsonSerializer.Serialize(new
        {
            truncated = true,
            originalLength = payloadJson.Length,
            preview = payloadJson[..Math.Min(4000, payloadJson.Length)]
        }, JsonOptions);
    }
}
