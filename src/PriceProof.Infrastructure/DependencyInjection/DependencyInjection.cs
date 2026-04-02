using System.IO;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PriceProof.Application.Abstractions.ComplaintPacks;
using PriceProof.Application.Abstractions.Diagnostics;
using PriceProof.Application.Abstractions.Ocr;
using PriceProof.Application.Abstractions.Persistence;
using PriceProof.Application.Abstractions.Security;
using PriceProof.Application.Abstractions.Services;
using PriceProof.Domain.Entities;
using PriceProof.Infrastructure.Auth;
using PriceProof.Infrastructure.ComplaintPacks;
using PriceProof.Infrastructure.Diagnostics;
using PriceProof.Infrastructure.Ocr;
using PriceProof.Infrastructure.Options;
using PriceProof.Infrastructure.Persistence;
using PriceProof.Infrastructure.Persistence.Interceptors;
using PriceProof.Infrastructure.Seeding;
using PriceProof.Infrastructure.Uploads;

namespace PriceProof.Infrastructure.DependencyInjection;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var dataProtectionPath = Path.Combine(Directory.GetCurrentDirectory(), ".appdata", "dataprotection");
        Directory.CreateDirectory(dataProtectionPath);

        services.AddDataProtection()
            .PersistKeysToFileSystem(new DirectoryInfo(dataProtectionPath))
            .SetApplicationName("PriceProof.Api");
        services.AddHttpContextAccessor();
        services.AddSingleton<AuditingInterceptor>();
        services.AddScoped<IRequestContextAccessor, HttpContextRequestContextAccessor>();
        services.AddScoped<ICurrentUserContext, HttpContextCurrentUserContext>();
        services.AddSingleton<IPasswordHashingService, Pbkdf2PasswordHashingService>();
        services.Configure<BootstrapAdminOptions>(configuration.GetSection(BootstrapAdminOptions.SectionName));
        services.Configure<ComplaintPackOptions>(configuration.GetSection(ComplaintPackOptions.SectionName));
        services.Configure<FileUploadOptions>(configuration.GetSection(FileUploadOptions.SectionName));
        services.Configure<OcrOptions>(configuration.GetSection(OcrOptions.SectionName));
        services.Configure<SessionAuthOptions>(configuration.GetSection(SessionAuthOptions.SectionName));

        services.AddDbContext<AppDbContext>((serviceProvider, options) =>
        {
            options.UseNpgsql(
                configuration.GetConnectionString("DefaultConnection"),
                builder => builder.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName));

            options.AddInterceptors(serviceProvider.GetRequiredService<AuditingInterceptor>());
        });

        services.AddScoped<IApplicationDbContext>(serviceProvider => serviceProvider.GetRequiredService<AppDbContext>());
        services.AddSingleton<ISessionTokenService, SessionTokenService>();
        services.AddSingleton<ReceiptOcrTextParser>();
        services.AddScoped<IComplaintPackGenerator, QuestPdfComplaintPackGenerator>();
        services.AddScoped<IComplaintPackDocumentStore, FileSystemComplaintPackDocumentStore>();
        services.AddScoped<IFileUploadService, FileSystemFileUploadService>();
        services.AddScoped<IReceiptDocumentContentResolver, FileSystemReceiptDocumentContentResolver>();
        services.AddScoped<IOcrOrchestrator, OcrOrchestrator>();

        services.AddHttpClient<AzureDocumentIntelligenceOcrProvider>(client =>
        {
            client.Timeout = Timeout.InfiniteTimeSpan;
        });
        services.AddHttpClient<GoogleVisionOcrProvider>(client =>
        {
            client.Timeout = Timeout.InfiniteTimeSpan;
        });

        services.AddScoped<IOcrProvider>(serviceProvider => serviceProvider.GetRequiredService<AzureDocumentIntelligenceOcrProvider>());
        services.AddScoped<IOcrProvider>(serviceProvider => serviceProvider.GetRequiredService<GoogleVisionOcrProvider>());

        services.AddHealthChecks()
            .AddDbContextCheck<AppDbContext>("postgresql");

        return services;
    }

    public static async Task MigrateDatabaseAsync(this IServiceProvider serviceProvider)
    {
        await using var scope = serviceProvider.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var environment = scope.ServiceProvider.GetRequiredService<IHostEnvironment>();
        var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();

        if (environment.IsEnvironment("Testing"))
        {
            await dbContext.Database.EnsureCreatedAsync();
            await EnsureBootstrapAdminAsync(scope.ServiceProvider, dbContext, environment, configuration, logger: null);
            return;
        }

        var logger = scope.ServiceProvider
            .GetService<ILoggerFactory>()?
            .CreateLogger("PriceProof.Infrastructure.Migrations");

        const int maxAttempts = 5;

        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                await dbContext.Database.MigrateAsync();
                await RepairKnownSchemaDriftAsync(dbContext, logger);
                await EnsureBootstrapAdminAsync(scope.ServiceProvider, dbContext, environment, configuration, logger);
                return;
            }
            catch (Exception exception) when (attempt < maxAttempts)
            {
                var delay = TimeSpan.FromSeconds(attempt * 2);
                logger?.LogWarning(
                    exception,
                    "Database migration attempt {Attempt} of {MaxAttempts} failed. Retrying in {DelaySeconds} seconds.",
                    attempt,
                    maxAttempts,
                    delay.TotalSeconds);

                await Task.Delay(delay);
            }
        }
    }

    private static async Task RepairKnownSchemaDriftAsync(AppDbContext dbContext, ILogger? logger)
    {
        const string compatibilitySql = """
                                        ALTER TABLE users ADD COLUMN IF NOT EXISTS "PasswordHash" character varying(256);
                                        ALTER TABLE users ADD COLUMN IF NOT EXISTS "PasswordSalt" character varying(128);
                                        ALTER TABLE users ADD COLUMN IF NOT EXISTS "PasswordIterations" integer;
                                        ALTER TABLE users ADD COLUMN IF NOT EXISTS "LastSignedInUtc" timestamp with time zone;

                                        ALTER TABLE cases ADD COLUMN IF NOT EXISTS "AnalysisClassification" integer;
                                        ALTER TABLE cases ADD COLUMN IF NOT EXISTS "AnalysisConfidence" numeric(5,4);
                                        ALTER TABLE cases ADD COLUMN IF NOT EXISTS "AnalysisExplanation" character varying(2000);
                                        ALTER TABLE cases ADD COLUMN IF NOT EXISTS "AnalysisUpdatedUtc" timestamp with time zone;

                                        ALTER TABLE receipt_records ADD COLUMN IF NOT EXISTS "OcrConfidence" numeric(5,4);
                                        ALTER TABLE receipt_records ADD COLUMN IF NOT EXISTS "OcrLineItemsJson" character varying(16000);
                                        ALTER TABLE receipt_records ADD COLUMN IF NOT EXISTS "OcrPayloadMetadataJson" character varying(32000);
                                        ALTER TABLE receipt_records ADD COLUMN IF NOT EXISTS "OcrProcessedUtc" timestamp with time zone;
                                        ALTER TABLE receipt_records ADD COLUMN IF NOT EXISTS "OcrProviderName" character varying(80);
                                        ALTER TABLE receipt_records ADD COLUMN IF NOT EXISTS "TransactionAtUtc" timestamp with time zone;

                                        ALTER TABLE merchants ADD COLUMN IF NOT EXISTS "CurrentRiskScore" numeric(5,2);
                                        ALTER TABLE merchants ADD COLUMN IF NOT EXISTS "CurrentRiskLabel" integer;
                                        ALTER TABLE merchants ADD COLUMN IF NOT EXISTS "RiskUpdatedUtc" timestamp with time zone;

                                        ALTER TABLE branches ADD COLUMN IF NOT EXISTS "CurrentRiskScore" numeric(5,2);
                                        ALTER TABLE branches ADD COLUMN IF NOT EXISTS "CurrentRiskLabel" integer;
                                        ALTER TABLE branches ADD COLUMN IF NOT EXISTS "RiskUpdatedUtc" timestamp with time zone;

                                        ALTER TABLE users ADD COLUMN IF NOT EXISTS "IsAdmin" boolean NOT NULL DEFAULT false;

                                        CREATE TABLE IF NOT EXISTS merchant_risk_snapshots (
                                            "Id" uuid NOT NULL,
                                            "MerchantId" uuid NOT NULL,
                                            "ModelVersion" character varying(32) NOT NULL,
                                            "TotalCases" integer NOT NULL,
                                            "AnalyzedCases" integer NOT NULL,
                                            "LikelyCardSurchargeCases" integer NOT NULL,
                                            "ConfidenceWeightedMismatchTotal" numeric(18,2) NOT NULL,
                                            "RecencyWeightedCaseCount" numeric(8,4) NOT NULL,
                                            "DismissedEquivalentRatio" numeric(5,4) NOT NULL,
                                            "UnclearCaseRatio" numeric(5,4) NOT NULL,
                                            "Score" numeric(5,2) NOT NULL,
                                            "Label" integer NOT NULL,
                                            "CalculatedUtc" timestamp with time zone NOT NULL,
                                            "TriggeredByCaseId" uuid NULL,
                                            "CreatedUtc" timestamp with time zone NOT NULL,
                                            "UpdatedUtc" timestamp with time zone NOT NULL,
                                            CONSTRAINT "PK_merchant_risk_snapshots" PRIMARY KEY ("Id"),
                                            CONSTRAINT "FK_merchant_risk_snapshots_merchants_MerchantId"
                                                FOREIGN KEY ("MerchantId") REFERENCES merchants ("Id") ON DELETE CASCADE
                                        );

                                        CREATE TABLE IF NOT EXISTS branch_risk_snapshots (
                                            "Id" uuid NOT NULL,
                                            "BranchId" uuid NOT NULL,
                                            "ModelVersion" character varying(32) NOT NULL,
                                            "TotalCases" integer NOT NULL,
                                            "AnalyzedCases" integer NOT NULL,
                                            "LikelyCardSurchargeCases" integer NOT NULL,
                                            "ConfidenceWeightedMismatchTotal" numeric(18,2) NOT NULL,
                                            "RecencyWeightedCaseCount" numeric(8,4) NOT NULL,
                                            "DismissedEquivalentRatio" numeric(5,4) NOT NULL,
                                            "UnclearCaseRatio" numeric(5,4) NOT NULL,
                                            "Score" numeric(5,2) NOT NULL,
                                            "Label" integer NOT NULL,
                                            "CalculatedUtc" timestamp with time zone NOT NULL,
                                            "TriggeredByCaseId" uuid NULL,
                                            "CreatedUtc" timestamp with time zone NOT NULL,
                                            "UpdatedUtc" timestamp with time zone NOT NULL,
                                            CONSTRAINT "PK_branch_risk_snapshots" PRIMARY KEY ("Id"),
                                            CONSTRAINT "FK_branch_risk_snapshots_branches_BranchId"
                                                FOREIGN KEY ("BranchId") REFERENCES branches ("Id") ON DELETE CASCADE
                                        );

                                        CREATE INDEX IF NOT EXISTS "IX_merchant_risk_snapshots_MerchantId_CalculatedUtc"
                                            ON merchant_risk_snapshots ("MerchantId", "CalculatedUtc");
                                        CREATE INDEX IF NOT EXISTS "IX_branch_risk_snapshots_BranchId_CalculatedUtc"
                                            ON branch_risk_snapshots ("BranchId", "CalculatedUtc");

                                        UPDATE users
                                        SET "IsAdmin" = true
                                        WHERE "Id" = '11111111-1111-1111-1111-111111111111';
                                        """;

        await dbContext.Database.ExecuteSqlRawAsync(compatibilitySql);
        logger?.LogInformation(
            "Ensured compatibility schema for analysis, OCR, and risk-scoring fields after migrations. Admin seed user: {AdminUserId}",
            SeedData.AdminUserId);
    }

    private static async Task EnsureBootstrapAdminAsync(
        IServiceProvider serviceProvider,
        AppDbContext dbContext,
        IHostEnvironment environment,
        IConfiguration configuration,
        ILogger? logger)
    {
        var options = serviceProvider.GetRequiredService<IOptions<BootstrapAdminOptions>>().Value;
        var passwordHashingService = serviceProvider.GetRequiredService<IPasswordHashingService>();
        var now = DateTimeOffset.UtcNow;

        if (environment.IsProduction())
        {
            var localSeedUsers = await dbContext.Users
                .Where(entity => entity.Email.EndsWith("@priceproof.local"))
                .ToListAsync();

            foreach (var localSeedUser in localSeedUsers.Where(entity => entity.IsActive))
            {
                localSeedUser.Deactivate(now);
            }

            await dbContext.SaveChangesAsync();
        }

        if (!string.IsNullOrWhiteSpace(options.Email) &&
            !string.IsNullOrWhiteSpace(options.DisplayName) &&
            !string.IsNullOrWhiteSpace(options.Password))
        {
            var normalizedEmail = options.Email.Trim().ToUpperInvariant();
            var existingAdmin = await dbContext.Users
                .SingleOrDefaultAsync(entity => entity.NormalizedEmail == normalizedEmail);
            var passwordHash = passwordHashingService.HashPassword(options.Password.Trim());

            if (existingAdmin is null)
            {
                existingAdmin = User.Create(options.DisplayName.Trim(), options.Email.Trim(), isAdmin: true);
                existingAdmin.SetPassword(passwordHash.PasswordHash, passwordHash.PasswordSalt, passwordHash.PasswordIterations, now);
                dbContext.Users.Add(existingAdmin);
                logger?.LogInformation("Created bootstrap admin account for {Email}.", options.Email.Trim());
            }
            else
            {
                existingAdmin.UpdateProfile(options.DisplayName.Trim(), options.Email.Trim(), now);
                existingAdmin.PromoteToAdmin(now);
                existingAdmin.Reactivate(now);
                existingAdmin.SetPassword(passwordHash.PasswordHash, passwordHash.PasswordSalt, passwordHash.PasswordIterations, now);
                logger?.LogInformation("Updated bootstrap admin account for {Email}.", options.Email.Trim());
            }

            await dbContext.SaveChangesAsync();
        }

        if (!environment.IsProduction())
        {
            return;
        }

        var activeAdminExists = await dbContext.Users.AnyAsync(entity => entity.IsActive && entity.IsAdmin);
        if (!activeAdminExists)
        {
            throw new InvalidOperationException(
                "No active admin account is configured for production. Supply the bootstrap admin settings through secure deployment configuration.");
        }

        ValidateProductionConfiguration(configuration);
    }

    private static void ValidateProductionConfiguration(IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection") ?? string.Empty;
        if (string.IsNullOrWhiteSpace(connectionString) ||
            connectionString.Contains("Password=priceproof", StringComparison.OrdinalIgnoreCase) ||
            connectionString.Contains("Host=localhost", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("A production database connection string must be supplied through secure configuration.");
        }

        var allowedOrigins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
        if (allowedOrigins.Any(origin => origin.Contains("localhost", StringComparison.OrdinalIgnoreCase) || origin.Contains("127.0.0.1")))
        {
            throw new InvalidOperationException("Production CORS origins cannot point to localhost.");
        }

        var ocrEnabled = configuration.GetValue<bool>("Ocr:Enabled");
        if (!ocrEnabled)
        {
            return;
        }

        var primaryProvider = configuration.GetValue<string>("Ocr:PrimaryProvider");
        if (string.Equals(primaryProvider, "AzureDocumentIntelligence", StringComparison.OrdinalIgnoreCase))
        {
            var endpoint = configuration["Ocr:AzureDocumentIntelligence:Endpoint"];
            var apiKey = configuration["Ocr:AzureDocumentIntelligence:ApiKey"];
            if (string.IsNullOrWhiteSpace(endpoint) || string.IsNullOrWhiteSpace(apiKey))
            {
                throw new InvalidOperationException("Azure Document Intelligence OCR is enabled but its secure configuration is incomplete.");
            }
        }

        if (string.Equals(primaryProvider, "GoogleVision", StringComparison.OrdinalIgnoreCase))
        {
            var apiKey = configuration["Ocr:GoogleVision:ApiKey"];
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                throw new InvalidOperationException("Google Vision OCR is enabled but its API key is missing.");
            }
        }
    }
}
