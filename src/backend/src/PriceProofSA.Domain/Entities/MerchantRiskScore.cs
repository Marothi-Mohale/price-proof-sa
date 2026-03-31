using PriceProofSA.Domain.Common;

namespace PriceProofSA.Domain.Entities;

public sealed class MerchantRiskScore : BaseEntity
{
    private MerchantRiskScore()
    {
    }

    public Guid MerchantId { get; private set; }

    public Merchant? Merchant { get; private set; }

    public int TotalReports { get; private set; }

    public int ConfirmedSurchargeSignals { get; private set; }

    public decimal Score { get; private set; }

    public string Trend { get; private set; } = "Low";

    public DateTimeOffset LastCalculatedAtUtc { get; private set; }

    public static MerchantRiskScore Create(Guid merchantId)
    {
        return new MerchantRiskScore
        {
            MerchantId = merchantId
        };
    }

    public void Update(int totalReports, int confirmedSurchargeSignals, decimal score, string trend, DateTimeOffset calculatedAtUtc)
    {
        TotalReports = totalReports;
        ConfirmedSurchargeSignals = confirmedSurchargeSignals;
        Score = score;
        Trend = trend;
        LastCalculatedAtUtc = calculatedAtUtc;
    }
}
