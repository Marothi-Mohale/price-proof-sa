using PriceProofSA.Domain.ValueObjects;

namespace PriceProofSA.Domain.Services;

public sealed class MerchantRiskCalculator
{
    public MerchantRiskSnapshot Calculate(int totalReports, int confirmedSurchargeSignals)
    {
        if (totalReports < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(totalReports));
        }

        if (confirmedSurchargeSignals < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(confirmedSurchargeSignals));
        }

        var ratio = totalReports == 0 ? 0m : (decimal)confirmedSurchargeSignals / totalReports;
        var score = Math.Min(100m, Math.Round((totalReports * 11m) + (confirmedSurchargeSignals * 17m) + (ratio * 25m), 2));
        var trend = score switch
        {
            >= 75m => "Critical",
            >= 50m => "High",
            >= 25m => "Moderate",
            _ => "Low"
        };

        return new MerchantRiskSnapshot(totalReports, confirmedSurchargeSignals, score, trend);
    }
}
