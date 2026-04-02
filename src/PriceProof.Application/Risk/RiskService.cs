using Microsoft.EntityFrameworkCore;
using PriceProof.Application.Abstractions.Persistence;
using PriceProof.Application.Abstractions.Services;
using PriceProof.Application.Common.Exceptions;
using PriceProof.Domain.Entities;
using PriceProof.Domain.Enums;
using PriceProof.Domain.Services;

namespace PriceProof.Application.Risk;

internal sealed class RiskService : IRiskService
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IRiskScoringEngine _riskScoringEngine;

    public RiskService(IApplicationDbContext dbContext, IRiskScoringEngine riskScoringEngine)
    {
        _dbContext = dbContext;
        _riskScoringEngine = riskScoringEngine;
    }

    public async Task RecalculateAsync(DiscrepancyCase discrepancyCase, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var merchant = await _dbContext.Merchants
            .Include(entity => entity.RiskSnapshots)
            .SingleOrDefaultAsync(entity => entity.Id == discrepancyCase.MerchantId, cancellationToken);

        if (merchant is null)
        {
            throw new NotFoundException($"Merchant '{discrepancyCase.MerchantId}' was not found.");
        }

        var merchantCases = await LoadCaseSignalsAsync(
            query => query.Where(entity => entity.MerchantId == discrepancyCase.MerchantId),
            cancellationToken);
        var merchantScore = _riskScoringEngine.Calculate(merchantCases, now);
        var merchantSnapshot = MerchantRiskSnapshot.Create(merchant.Id, merchantScore, now, discrepancyCase.Id);
        merchant.ApplyRiskSnapshot(merchantSnapshot, now);
        _dbContext.MerchantRiskSnapshots.Add(merchantSnapshot);

        if (!discrepancyCase.BranchId.HasValue)
        {
            return;
        }

        var branch = await _dbContext.Branches
            .Include(entity => entity.RiskSnapshots)
            .SingleOrDefaultAsync(entity => entity.Id == discrepancyCase.BranchId.Value, cancellationToken);

        if (branch is null)
        {
            throw new NotFoundException($"Branch '{discrepancyCase.BranchId.Value}' was not found.");
        }

        var branchCases = await LoadCaseSignalsAsync(
            query => query.Where(entity => entity.BranchId == discrepancyCase.BranchId.Value),
            cancellationToken);
        var branchScore = _riskScoringEngine.Calculate(branchCases, now);
        var branchSnapshot = BranchRiskSnapshot.Create(branch.Id, branchScore, now, discrepancyCase.Id);
        branch.ApplyRiskSnapshot(branchSnapshot, now);
        _dbContext.BranchRiskSnapshots.Add(branchSnapshot);
    }

    public async Task<MerchantRiskDto> GetMerchantRiskAsync(Guid merchantId, CancellationToken cancellationToken)
    {
        var merchant = await _dbContext.Merchants
            .AsNoTracking()
            .Include(entity => entity.RiskSnapshots)
            .SingleOrDefaultAsync(entity => entity.Id == merchantId, cancellationToken);

        if (merchant is null)
        {
            throw new NotFoundException($"Merchant '{merchantId}' was not found.");
        }

        var latestSnapshot = merchant.RiskSnapshots
            .OrderByDescending(snapshot => snapshot.CalculatedUtc)
            .ThenByDescending(snapshot => snapshot.CreatedUtc)
            .FirstOrDefault();

        return new MerchantRiskDto(
            merchant.Id,
            merchant.Name,
            merchant.Category,
            merchant.WebsiteUrl,
            merchant.CurrentRiskScore ?? 0m,
            (merchant.CurrentRiskLabel ?? RiskLabel.Low).ToString(),
            latestSnapshot?.TotalCases ?? 0,
            latestSnapshot?.AnalyzedCases ?? 0,
            latestSnapshot?.LikelyCardSurchargeCases ?? 0,
            latestSnapshot?.ConfidenceWeightedMismatchTotal ?? 0m,
            latestSnapshot?.RecencyWeightedCaseCount ?? 0m,
            latestSnapshot?.DismissedEquivalentRatio ?? 0m,
            latestSnapshot?.UnclearCaseRatio ?? 0m,
            merchant.RiskUpdatedUtc,
            merchant.RiskSnapshots
                .OrderByDescending(snapshot => snapshot.CalculatedUtc)
                .ThenByDescending(snapshot => snapshot.CreatedUtc)
                .Take(12)
                .Select(MapSnapshot)
                .ToArray());
    }

    public async Task<BranchRiskDto> GetBranchRiskAsync(Guid branchId, CancellationToken cancellationToken)
    {
        var branch = await _dbContext.Branches
            .AsNoTracking()
            .Include(entity => entity.Merchant)
            .Include(entity => entity.RiskSnapshots)
            .SingleOrDefaultAsync(entity => entity.Id == branchId, cancellationToken);

        if (branch is null)
        {
            throw new NotFoundException($"Branch '{branchId}' was not found.");
        }

        var latestSnapshot = branch.RiskSnapshots
            .OrderByDescending(snapshot => snapshot.CalculatedUtc)
            .ThenByDescending(snapshot => snapshot.CreatedUtc)
            .FirstOrDefault();

        return new BranchRiskDto(
            branch.Id,
            branch.MerchantId,
            branch.Name,
            branch.Merchant?.Name ?? "Unknown merchant",
            branch.City,
            branch.Province,
            branch.CurrentRiskScore ?? 0m,
            (branch.CurrentRiskLabel ?? RiskLabel.Low).ToString(),
            latestSnapshot?.TotalCases ?? 0,
            latestSnapshot?.AnalyzedCases ?? 0,
            latestSnapshot?.LikelyCardSurchargeCases ?? 0,
            latestSnapshot?.ConfidenceWeightedMismatchTotal ?? 0m,
            latestSnapshot?.RecencyWeightedCaseCount ?? 0m,
            latestSnapshot?.DismissedEquivalentRatio ?? 0m,
            latestSnapshot?.UnclearCaseRatio ?? 0m,
            branch.RiskUpdatedUtc,
            branch.RiskSnapshots
                .OrderByDescending(snapshot => snapshot.CalculatedUtc)
                .ThenByDescending(snapshot => snapshot.CreatedUtc)
                .Take(12)
                .Select(MapSnapshot)
                .ToArray());
    }

    public async Task<RiskOverviewDto> GetOverviewAsync(Guid requestedByUserId, CancellationToken cancellationToken)
    {
        var user = await _dbContext.Users
            .AsNoTracking()
            .SingleOrDefaultAsync(entity => entity.Id == requestedByUserId, cancellationToken);

        if (user is null)
        {
            throw new NotFoundException($"User '{requestedByUserId}' was not found.");
        }

        if (!user.IsAdmin)
        {
            throw new ForbiddenException("Only admin users can view the risk overview.");
        }

        var merchants = await _dbContext.Merchants
            .AsNoTracking()
            .Include(entity => entity.RiskSnapshots)
            .Where(entity => entity.CurrentRiskScore.HasValue)
            .OrderByDescending(entity => entity.CurrentRiskScore ?? 0m)
            .ThenByDescending(entity => entity.RiskUpdatedUtc)
            .ThenBy(entity => entity.Name)
            .Take(10)
            .ToListAsync(cancellationToken);

        var branches = await _dbContext.Branches
            .AsNoTracking()
            .Include(entity => entity.Merchant)
            .Include(entity => entity.RiskSnapshots)
            .Where(entity => entity.CurrentRiskScore.HasValue)
            .OrderByDescending(entity => entity.CurrentRiskScore ?? 0m)
            .ThenByDescending(entity => entity.RiskUpdatedUtc)
            .ThenBy(entity => entity.Name)
            .Take(10)
            .ToListAsync(cancellationToken);

        return new RiskOverviewDto(
            merchants.Select(merchant =>
            {
                var snapshot = merchant.RiskSnapshots
                    .OrderByDescending(item => item.CalculatedUtc)
                    .ThenByDescending(item => item.CreatedUtc)
                    .FirstOrDefault();

                return new RiskLeaderboardMerchantDto(
                    merchant.Id,
                    merchant.Name,
                    merchant.Category,
                    merchant.CurrentRiskScore ?? 0m,
                    (merchant.CurrentRiskLabel ?? RiskLabel.Low).ToString(),
                    snapshot?.TotalCases ?? 0,
                    snapshot?.LikelyCardSurchargeCases ?? 0,
                    merchant.RiskUpdatedUtc);
            }).ToArray(),
            branches.Select(branch =>
            {
                var snapshot = branch.RiskSnapshots
                    .OrderByDescending(item => item.CalculatedUtc)
                    .ThenByDescending(item => item.CreatedUtc)
                    .FirstOrDefault();

                return new RiskLeaderboardBranchDto(
                    branch.Id,
                    branch.MerchantId,
                    branch.Name,
                    branch.Merchant?.Name ?? "Unknown merchant",
                    branch.City,
                    branch.Province,
                    branch.CurrentRiskScore ?? 0m,
                    (branch.CurrentRiskLabel ?? RiskLabel.Low).ToString(),
                    snapshot?.TotalCases ?? 0,
                    snapshot?.LikelyCardSurchargeCases ?? 0,
                    branch.RiskUpdatedUtc);
            }).ToArray());
    }

    private async Task<RiskCaseSignal[]> LoadCaseSignalsAsync(
        Func<IQueryable<DiscrepancyCase>, IQueryable<DiscrepancyCase>> applyFilter,
        CancellationToken cancellationToken)
    {
        var cases = await applyFilter(_dbContext.DiscrepancyCases)
            .Select(entity => new RiskCaseSignal(
                entity.Id,
                entity.AnalysisClassification,
                entity.AnalysisConfidence,
                entity.DifferenceAmount,
                entity.IncidentAtUtc,
                entity.AnalysisUpdatedUtc))
            .ToListAsync(cancellationToken);

        return cases.ToArray();
    }

    private static RiskSnapshotDto MapSnapshot(MerchantRiskSnapshot snapshot)
    {
        return new RiskSnapshotDto(
            snapshot.ModelVersion,
            snapshot.TotalCases,
            snapshot.AnalyzedCases,
            snapshot.LikelyCardSurchargeCases,
            snapshot.ConfidenceWeightedMismatchTotal,
            snapshot.RecencyWeightedCaseCount,
            snapshot.DismissedEquivalentRatio,
            snapshot.UnclearCaseRatio,
            snapshot.Score,
            snapshot.Label.ToString(),
            snapshot.CalculatedUtc);
    }

    private static RiskSnapshotDto MapSnapshot(BranchRiskSnapshot snapshot)
    {
        return new RiskSnapshotDto(
            snapshot.ModelVersion,
            snapshot.TotalCases,
            snapshot.AnalyzedCases,
            snapshot.LikelyCardSurchargeCases,
            snapshot.ConfidenceWeightedMismatchTotal,
            snapshot.RecencyWeightedCaseCount,
            snapshot.DismissedEquivalentRatio,
            snapshot.UnclearCaseRatio,
            snapshot.Score,
            snapshot.Label.ToString(),
            snapshot.CalculatedUtc);
    }
}
