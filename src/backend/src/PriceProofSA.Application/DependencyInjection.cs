using Microsoft.Extensions.DependencyInjection;
using PriceProofSA.Application.Auth;
using PriceProofSA.Application.Services;
using PriceProofSA.Domain.Services;

namespace PriceProofSA.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<DiscrepancyClassifier>();
        services.AddScoped<MerchantRiskCalculator>();
        services.AddScoped<AuthService>();
        services.AddScoped<CaseService>();
        services.AddScoped<MerchantService>();

        return services;
    }
}
