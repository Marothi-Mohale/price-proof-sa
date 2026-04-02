using Microsoft.Extensions.DependencyInjection;
using PriceProof.Application.Auth;
using PriceProof.Application.Abstractions.Services;
using PriceProof.Application.Cases;
using PriceProof.Application.ComplaintPacks;
using PriceProof.Application.Lookups;
using PriceProof.Application.Merchants;
using PriceProof.Application.PaymentRecords;
using PriceProof.Application.PriceCaptures;
using PriceProof.Application.ReceiptRecords;
using PriceProof.Application.Risk;
using PriceProof.Domain.Services;

namespace PriceProof.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddSingleton<IDiscrepancyDetectionEngine>(_ => new DiscrepancyDetectionEngine());
        services.AddSingleton<IRiskScoringEngine, RiskScoringEngine>();
        services.AddSingleton<IComplaintNarrativeComposer, ComplaintNarrativeComposer>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<ICaseService, CaseService>();
        services.AddScoped<IComplaintPackService, ComplaintPackService>();
        services.AddScoped<ILookupService, LookupService>();
        services.AddScoped<IMerchantService, MerchantService>();
        services.AddScoped<IRiskService, RiskService>();
        services.AddScoped<IPriceCaptureService, PriceCaptureService>();
        services.AddScoped<IPaymentRecordService, PaymentRecordService>();
        services.AddScoped<IReceiptRecordService, ReceiptRecordService>();

        return services;
    }
}
