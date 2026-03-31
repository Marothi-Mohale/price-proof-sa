using Microsoft.EntityFrameworkCore;
using PriceProofSA.Application.Abstractions.Risk;
using PriceProofSA.Application.Abstractions.Time;
using PriceProofSA.Domain.Entities;
using PriceProofSA.Domain.Enums;
using PriceProofSA.Domain.Services;
using PriceProofSA.Infrastructure.Persistence;

namespace PriceProofSA.Infrastructure.Risk;

public sealed class MerchantRiskScoringService : IMerchantRiskScoringService
{
    private readonly PriceProofDbContext _dbContext;
    private readonly MerchantRiskCalculator _calculator;
    private readonly IClock _clock;

    public MerchantRiskScoringService(PriceProofDbContext dbContext, MerchantRiskCalculator calculator, IClock clock)
    {
        _dbContext = dbContext;
        _calculator = calculator;
        _clock = clock;
    }

    public async Task<MerchantRiskSummary> RecalculateAsync(Guid merchantId, CancellationToken cancellationToken = default)
    {
        var totalReports = await _dbContext.Cases.CountAsync(item => item.MerchantId == merchantId, cancellationToken);
        var confirmedSignals = await _dbContext.Cases.CountAsync(
            item => item.MerchantId == merchantId && item.Classification == DiscrepancyClassification.LikelyCardSurcharge,
            cancellationToken);

        var snapshot = _calculator.Calculate(totalReports, confirmedSignals);
        var riskScore = await _dbContext.MerchantRiskScores.SingleOrDefaultAsync(item => item.MerchantId == merchantId, cancellationToken)
            ?? MerchantRiskScore.Create(merchantId);

        if (_dbContext.Entry(riskScore).State == EntityState.Detached)
        {
            await _dbContext.MerchantRiskScores.AddAsync(riskScore, cancellationToken);
        }

        riskScore.Update(snapshot.TotalReports, snapshot.ConfirmedSurchargeSignals, snapshot.Score, snapshot.Trend, _clock.UtcNow);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new MerchantRiskSummary(
            merchantId,
            snapshot.TotalReports,
            snapshot.ConfirmedSurchargeSignals,
            snapshot.Score,
            snapshot.Trend,
            riskScore.LastCalculatedAtUtc);
    }
}
