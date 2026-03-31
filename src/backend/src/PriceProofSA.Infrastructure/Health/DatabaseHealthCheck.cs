using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using PriceProofSA.Infrastructure.Persistence;

namespace PriceProofSA.Infrastructure.Health;

public sealed class DatabaseHealthCheck : IHealthCheck
{
    private readonly PriceProofDbContext _dbContext;

    public DatabaseHealthCheck(PriceProofDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Database.CanConnectAsync(cancellationToken)
            ? HealthCheckResult.Healthy("Database is reachable.")
            : HealthCheckResult.Unhealthy("Database is not reachable.");
    }
}
