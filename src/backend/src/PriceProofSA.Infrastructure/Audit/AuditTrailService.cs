using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using PriceProofSA.Application.Abstractions.Audit;
using PriceProofSA.Application.Abstractions.Time;
using PriceProofSA.Domain.Entities;
using PriceProofSA.Domain.Services;
using PriceProofSA.Infrastructure.Persistence;

namespace PriceProofSA.Infrastructure.Audit;

public sealed class AuditTrailService : IAuditTrailService
{
    private readonly PriceProofDbContext _dbContext;
    private readonly IClock _clock;

    public AuditTrailService(PriceProofDbContext dbContext, IClock clock)
    {
        _dbContext = dbContext;
        _clock = clock;
    }

    public async Task<AuditLog> BuildEntryAsync(AuditEntryInput input, CancellationToken cancellationToken = default)
    {
        var previousHash = await _dbContext.AuditLogs
            .OrderByDescending(item => item.OccurredAtUtc)
            .ThenByDescending(item => item.Id)
            .Select(item => item.Hash)
            .FirstOrDefaultAsync(cancellationToken);

        var payloadJson = JsonSerializer.Serialize(input.Payload);
        var now = _clock.UtcNow;
        var hash = AuditHashCalculator.Compute(
            previousHash,
            input.UserId,
            input.CaseId,
            input.EntityType,
            input.EntityId,
            input.Action,
            payloadJson,
            now);

        return AuditLog.Create(
            input.UserId,
            input.CaseId,
            input.EntityType,
            input.EntityId,
            input.Action,
            payloadJson,
            hash,
            previousHash,
            now);
    }
}
