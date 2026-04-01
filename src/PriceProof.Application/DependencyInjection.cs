using Microsoft.Extensions.DependencyInjection;
using PriceProof.Application.Abstractions.Services;
using PriceProof.Application.Cases;
using PriceProof.Application.ComplaintPacks;
using PriceProof.Application.PaymentRecords;
using PriceProof.Application.PriceCaptures;
using PriceProof.Application.ReceiptRecords;
using PriceProof.Domain.Services;

namespace PriceProof.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddSingleton<IDiscrepancyDetectionEngine>(_ => new DiscrepancyDetectionEngine());
        services.AddSingleton<IComplaintNarrativeComposer, ComplaintNarrativeComposer>();
        services.AddScoped<ICaseService, CaseService>();
        services.AddScoped<IComplaintPackService, ComplaintPackService>();
        services.AddScoped<IPriceCaptureService, PriceCaptureService>();
        services.AddScoped<IPaymentRecordService, PaymentRecordService>();
        services.AddScoped<IReceiptRecordService, ReceiptRecordService>();

        return services;
    }
}
