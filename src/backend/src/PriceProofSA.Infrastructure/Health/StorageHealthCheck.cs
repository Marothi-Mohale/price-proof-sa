using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using PriceProofSA.Infrastructure.Options;

namespace PriceProofSA.Infrastructure.Health;

public sealed class StorageHealthCheck : IHealthCheck
{
    private readonly StorageOptions _options;

    public StorageHealthCheck(IOptions<StorageOptions> options)
    {
        _options = options.Value;
    }

    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        if (_options.Provider.Equals("AzureBlob", StringComparison.OrdinalIgnoreCase))
        {
            return Task.FromResult(!string.IsNullOrWhiteSpace(_options.AzureBlobConnectionString)
                ? HealthCheckResult.Healthy("Azure Blob Storage is configured.")
                : HealthCheckResult.Degraded("Azure Blob Storage provider selected but the connection string is missing."));
        }

        var rootPath = Path.GetFullPath(_options.LocalRootPath);
        Directory.CreateDirectory(rootPath);
        return Task.FromResult(HealthCheckResult.Healthy($"Local storage is available at {rootPath}."));
    }
}
