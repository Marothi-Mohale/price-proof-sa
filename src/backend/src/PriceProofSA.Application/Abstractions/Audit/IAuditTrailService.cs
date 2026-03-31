using PriceProofSA.Domain.Entities;

namespace PriceProofSA.Application.Abstractions.Audit;

public interface IAuditTrailService
{
    Task<AuditLog> BuildEntryAsync(AuditEntryInput input, CancellationToken cancellationToken = default);
}
