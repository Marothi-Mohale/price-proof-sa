using Hangfire;
using Hangfire.InMemory;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PriceProofSA.Application.Abstractions.Audit;
using PriceProofSA.Application.Abstractions.Complaints;
using PriceProofSA.Application.Abstractions.Ocr;
using PriceProofSA.Application.Abstractions.Persistence;
using PriceProofSA.Application.Abstractions.Risk;
using PriceProofSA.Application.Abstractions.Storage;
using PriceProofSA.Application.Abstractions.Time;
using PriceProofSA.Infrastructure.Audit;
using PriceProofSA.Infrastructure.Auth;
using PriceProofSA.Infrastructure.BackgroundJobs;
using PriceProofSA.Infrastructure.Complaints;
using PriceProofSA.Infrastructure.Health;
using PriceProofSA.Infrastructure.Ocr;
using PriceProofSA.Infrastructure.Options;
using PriceProofSA.Infrastructure.Persistence;
using PriceProofSA.Infrastructure.Risk;
using PriceProofSA.Infrastructure.Storage;
using PriceProofSA.Infrastructure.Time;

namespace PriceProofSA.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<AuthOptions>(configuration.GetSection(AuthOptions.SectionName));
        services.Configure<StorageOptions>(configuration.GetSection(StorageOptions.SectionName));
        services.Configure<OcrOptions>(configuration.GetSection(OcrOptions.SectionName));

        services.AddDbContext<PriceProofDbContext>(options =>
        {
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection"));
        });

        services.AddScoped<IApplicationDbContext>(provider => provider.GetRequiredService<PriceProofDbContext>());
        services.AddSingleton<IClock, PriceProofSA.Infrastructure.Time.SystemClock>();
        services.AddScoped<IAuditTrailService, AuditTrailService>();
        services.AddScoped<IMerchantRiskScoringService, MerchantRiskScoringService>();
        services.AddScoped<IComplaintPackGenerator, SimplePdfComplaintPackGenerator>();

        services.AddScoped<ReceiptTotalParser>();
        services.AddScoped<MockOcrProvider>();
        services.AddHttpClient<AzureDocumentIntelligenceOcrProvider>();
        services.AddHttpClient<GoogleVisionOcrProvider>();
        services.AddScoped<IOcrProvider>(provider => provider.GetRequiredService<AzureDocumentIntelligenceOcrProvider>());
        services.AddScoped<IOcrProvider>(provider => provider.GetRequiredService<GoogleVisionOcrProvider>());
        services.AddScoped<IOcrProvider>(provider => provider.GetRequiredService<MockOcrProvider>());
        services.AddScoped<IOcrOrchestrator, FallbackOcrOrchestrator>();

        services.AddScoped<LocalFileStorageService>();
        services.AddScoped<AzureBlobFileStorageService>();
        services.AddScoped<RoutedFileStorageService>();
        services.AddScoped<IFileStorageService>(provider => provider.GetRequiredService<RoutedFileStorageService>());

        services.AddScoped<ReceiptOcrJob>();

        services.AddAuthentication(SessionTokenAuthenticationHandler.SchemeName)
            .AddScheme<AuthenticationSchemeOptions, SessionTokenAuthenticationHandler>(
                SessionTokenAuthenticationHandler.SchemeName,
                _ => { });

        services.AddAuthorization(options =>
        {
            options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
        });

        services.AddHangfire(configuration => configuration.UseInMemoryStorage(new InMemoryStorageOptions()));
        services.AddHangfireServer();

        services.AddHealthChecks()
            .AddCheck<DatabaseHealthCheck>("database")
            .AddCheck<StorageHealthCheck>("storage");

        return services;
    }

    public static async Task InitializeInfrastructureAsync(this IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<PriceProofDbContext>();
        await dbContext.Database.EnsureCreatedAsync();
    }
}
