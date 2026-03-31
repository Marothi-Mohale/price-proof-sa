using Microsoft.EntityFrameworkCore;
using PriceProofSA.Application.Abstractions.Persistence;
using PriceProofSA.Application.Cases;
using PriceProofSA.Application.Merchants;
using PriceProofSA.Domain.Enums;

namespace PriceProofSA.Application.Services;

public sealed class MerchantService
{
    private readonly IApplicationDbContext _dbContext;

    public MerchantService(IApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<MerchantHistoryDto?> GetMerchantHistoryAsync(Guid merchantId, CancellationToken cancellationToken = default)
    {
        var merchant = await _dbContext.Merchants
            .Include(static item => item.RiskScore)
            .SingleOrDefaultAsync(item => item.Id == merchantId, cancellationToken);

        if (merchant is null)
        {
            return null;
        }

        var recentCases = await _dbContext.Cases
            .Where(item => item.MerchantId == merchantId)
            .OrderByDescending(item => item.UpdatedAtUtc)
            .Take(12)
            .Select(item => new CaseListItemDto(
                item.Id,
                merchant.Name,
                item.BasketDescription,
                item.Status.ToString(),
                item.QuotedAmount,
                item.ChargedAmount,
                item.DifferenceAmount,
                item.Classification.ToString(),
                item.LikelyUnlawfulCardSurcharge,
                item.UpdatedAtUtc))
            .ToListAsync(cancellationToken);

        var totalCases = await _dbContext.Cases.CountAsync(item => item.MerchantId == merchantId, cancellationToken);
        var flaggedCases = await _dbContext.Cases.CountAsync(
            item => item.MerchantId == merchantId && item.Classification == DiscrepancyClassification.LikelyCardSurcharge,
            cancellationToken);

        return new MerchantHistoryDto(
            merchant.Id,
            merchant.Name,
            merchant.RiskScore is null
                ? null
                : new MerchantRiskDto(
                    merchant.RiskScore.TotalReports,
                    merchant.RiskScore.ConfirmedSurchargeSignals,
                    merchant.RiskScore.Score,
                    merchant.RiskScore.Trend,
                    merchant.RiskScore.LastCalculatedAtUtc),
            totalCases,
            flaggedCases,
            recentCases);
    }
}
