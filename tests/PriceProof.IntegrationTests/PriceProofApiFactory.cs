using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using PriceProof.Application.Abstractions.Ocr;
using PriceProof.Application.Abstractions.Persistence;
using PriceProof.Infrastructure.Options;
using PriceProof.Infrastructure.Persistence;
using PriceProof.Infrastructure.Persistence.Interceptors;
using PriceProof.IntegrationTests.Fakes;

namespace PriceProof.IntegrationTests;

public sealed class PriceProofApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private SqliteConnection? _connection;
    private string? _storageRootPath;

    public string StorageRootPath => _storageRootPath
        ?? throw new InvalidOperationException("The test storage path has not been initialized.");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        _storageRootPath = Path.Combine(Path.GetTempPath(), "priceproof-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_storageRootPath);

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<AppDbContext>>();
            services.RemoveAll<AppDbContext>();
            services.RemoveAll<IApplicationDbContext>();
            services.RemoveAll<AuditingInterceptor>();
            services.RemoveAll<IOcrProvider>();
            services.RemoveAll<IReceiptDocumentContentResolver>();

            _connection = new SqliteConnection("Data Source=:memory:");
            _connection.Open();

            services.AddSingleton(_connection);
            services.AddSingleton<AuditingInterceptor>();
            services.AddDbContext<AppDbContext>((serviceProvider, options) =>
            {
                options.UseSqlite(_connection);
                options.AddInterceptors(serviceProvider.GetRequiredService<AuditingInterceptor>());
            });
            services.AddScoped<IApplicationDbContext>(serviceProvider => serviceProvider.GetRequiredService<AppDbContext>());
            services.AddScoped<IOcrProvider, FakeOcrProvider>();
            services.AddScoped<IReceiptDocumentContentResolver, FakeReceiptDocumentContentResolver>();
            services.PostConfigure<OcrOptions>(options =>
            {
                options.Enabled = true;
                options.PrimaryProvider = "AzureDocumentIntelligence";
                options.SecondaryProvider = "GoogleVision";
                options.RequestTimeoutSeconds = 5;
                options.RetryCount = 0;
                options.StorageRootPath = _storageRootPath!;
            });
            services.PostConfigure<ComplaintPackOptions>(options =>
            {
                options.Enabled = true;
                options.StorageRootPath = _storageRootPath!;
                options.IncludeEvidencePreviews = true;
                options.IncludeEvidenceReferences = true;
            });
        });
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public new async Task DisposeAsync()
    {
        if (_connection is not null)
        {
            await _connection.DisposeAsync();
        }

        if (!string.IsNullOrWhiteSpace(_storageRootPath) && Directory.Exists(_storageRootPath))
        {
            Directory.Delete(_storageRootPath, recursive: true);
        }
    }
}
