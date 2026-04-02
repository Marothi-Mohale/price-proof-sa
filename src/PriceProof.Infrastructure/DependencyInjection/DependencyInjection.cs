using System.IO;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PriceProof.Application.Abstractions.ComplaintPacks;
using PriceProof.Application.Abstractions.Diagnostics;
using PriceProof.Application.Abstractions.Ocr;
using PriceProof.Application.Abstractions.Persistence;
using PriceProof.Application.Abstractions.Security;
using PriceProof.Application.Abstractions.Services;
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
        services.Configure<ComplaintPackOptions>(configuration.GetSection(ComplaintPackOptions.SectionName));
        services.Configure<FileUploadOptions>(configuration.GetSection(FileUploadOptions.SectionName));
        services.Configure<OcrOptions>(configuration.GetSection(OcrOptions.SectionName));

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

        if (environment.IsEnvironment("Testing"))
        {
            await dbContext.Database.EnsureCreatedAsync();
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
}
