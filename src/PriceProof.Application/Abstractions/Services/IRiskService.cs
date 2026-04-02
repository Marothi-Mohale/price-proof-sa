using PriceProof.Application.Risk;
using PriceProof.Domain.Entities;

namespace PriceProof.Application.Abstractions.Services;

public interface IRiskService
{
    Task RecalculateAsync(DiscrepancyCase discrepancyCase, DateTimeOffset now, CancellationToken cancellationToken);

    Task<MerchantRiskDto> GetMerchantRiskAsync(Guid merchantId, CancellationToken cancellationToken);

    Task<BranchRiskDto> GetBranchRiskAsync(Guid branchId, CancellationToken cancellationToken);

    Task<RiskOverviewDto> GetOverviewAsync(CancellationToken cancellationToken);
}
