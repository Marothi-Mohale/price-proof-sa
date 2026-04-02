using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using PriceProof.Application.Abstractions.Ocr;
using PriceProof.Application.Abstractions.Persistence;
using PriceProof.Application.Abstractions.Communication;
using PriceProof.Application.Auth;
using PriceProof.Infrastructure.Options;
using PriceProof.Infrastructure.Persistence;
using PriceProof.Infrastructure.Persistence.Interceptors;
using PriceProof.IntegrationTests.Fakes;

namespace PriceProof.IntegrationTests;

public sealed class PriceProofApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    public const string BootstrapAdminEmail = "admin.integration@priceproof.test";
    public const string BootstrapAdminPassword = "Admin!SecurePass123";
    public const string BootstrapAdminDisplayName = "Integration Admin";

    private SqliteConnection? _connection;
    private string? _storageRootPath;
    private FakeEmailDeliveryService? _fakeEmailDeliveryService;

    public string StorageRootPath => _storageRootPath
        ?? throw new InvalidOperationException("The test storage path has not been initialized.");

    public FakeEmailDeliveryService EmailDeliveryService => _fakeEmailDeliveryService
        ?? throw new InvalidOperationException("The fake email delivery service has not been initialized.");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        _storageRootPath = Path.Combine(Path.GetTempPath(), "priceproof-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_storageRootPath);

        builder.ConfigureServices(services =>
        {
            services.AddDataProtection().UseEphemeralDataProtectionProvider();
            services.RemoveAll<DbContextOptions<AppDbContext>>();
            services.RemoveAll<AppDbContext>();
            services.RemoveAll<IApplicationDbContext>();
            services.RemoveAll<AuditingInterceptor>();
            services.RemoveAll<IOcrProvider>();
            services.RemoveAll<IReceiptDocumentContentResolver>();
            services.RemoveAll<IEmailDeliveryService>();

            _connection = new SqliteConnection("Data Source=:memory:");
            _connection.Open();
            _fakeEmailDeliveryService = new FakeEmailDeliveryService();

            services.AddSingleton(_connection);
            services.AddSingleton(_fakeEmailDeliveryService);
            services.AddSingleton<AuditingInterceptor>();
            services.AddDbContext<AppDbContext>((serviceProvider, options) =>
            {
                options.UseSqlite(_connection);
                options.AddInterceptors(serviceProvider.GetRequiredService<AuditingInterceptor>());
            });
            services.AddScoped<IApplicationDbContext>(serviceProvider => serviceProvider.GetRequiredService<AppDbContext>());
            services.AddScoped<IOcrProvider, FakeOcrProvider>();
            services.AddScoped<IReceiptDocumentContentResolver, FakeReceiptDocumentContentResolver>();
            services.AddSingleton<IEmailDeliveryService>(serviceProvider => serviceProvider.GetRequiredService<FakeEmailDeliveryService>());
            services.PostConfigure<OcrOptions>(options =>
            {
                options.Enabled = true;
                options.PrimaryProvider = "AzureDocumentIntelligence";
                options.SecondaryProvider = "GoogleVision";
                options.RequestTimeoutSeconds = 5;
                options.RetryCount = 0;
                options.ProviderRetryCount = 0;
            });
            services.PostConfigure<ComplaintPackOptions>(options =>
            {
                options.Enabled = true;
                options.IncludeEvidencePreviews = true;
                options.IncludeEvidenceReferences = true;
            });
            services.PostConfigure<AccountSecurityOptions>(options =>
            {
                options.PublicAppUrl = "http://localhost:3000";
                options.RequireVerifiedEmailForSignIn = true;
                options.EmailVerificationTokenLifetimeHours = 4;
                options.PasswordResetTokenLifetimeMinutes = 30;
                options.MaxFailedSignInAttempts = 3;
                options.LockoutDurationMinutes = 5;
            });
            services.PostConfigure<BootstrapAdminOptions>(options =>
            {
                options.Email = BootstrapAdminEmail;
                options.DisplayName = BootstrapAdminDisplayName;
                options.Password = BootstrapAdminPassword;
            });
            services.PostConfigure<FileUploadOptions>(options =>
            {
                options.MaxFileSizeBytes = 1 * 1024 * 1024;
            });
            services.PostConfigure<SharedStorageOptions>(options =>
            {
                options.EnableLegacyFileFallback = false;
                options.LegacyFileRootPath = _storageRootPath!;
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
