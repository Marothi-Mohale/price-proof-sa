using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PriceProof.Application.Abstractions.Persistence;
using PriceProof.Infrastructure.Persistence;
using PriceProof.Infrastructure.Persistence.Interceptors;

namespace PriceProof.Infrastructure.DependencyInjection;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<AuditingInterceptor>();

        services.AddDbContext<AppDbContext>((serviceProvider, options) =>
        {
            options.UseNpgsql(
                configuration.GetConnectionString("DefaultConnection"),
                builder => builder.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName));

            options.AddInterceptors(serviceProvider.GetRequiredService<AuditingInterceptor>());
        });

        services.AddScoped<IApplicationDbContext>(serviceProvider => serviceProvider.GetRequiredService<AppDbContext>());

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
}
