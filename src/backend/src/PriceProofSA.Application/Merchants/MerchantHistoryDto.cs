using PriceProofSA.Application.Cases;

namespace PriceProofSA.Application.Merchants;

public sealed record MerchantHistoryDto(
    Guid MerchantId,
    string MerchantName,
    MerchantRiskDto? Risk,
    int TotalCases,
    int CasesFlaggedAsLikelyCardSurcharge,
    IReadOnlyCollection<CaseListItemDto> RecentCases);
