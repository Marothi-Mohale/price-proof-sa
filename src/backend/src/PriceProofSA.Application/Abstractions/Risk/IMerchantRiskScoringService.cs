namespace PriceProofSA.Application.Abstractions.Risk;

public interface IMerchantRiskScoringService
{
    Task<MerchantRiskSummary> RecalculateAsync(Guid merchantId, CancellationToken cancellationToken = default);
}
