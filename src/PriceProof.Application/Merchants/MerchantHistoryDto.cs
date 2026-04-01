using PriceProof.Application.Cases;

namespace PriceProof.Application.Merchants;

public sealed record MerchantHistoryDto(
    Guid MerchantId,
    string MerchantName,
    string? Category,
    string? WebsiteUrl,
    int TotalCases,
    int PotentialCardSurchargeCases,
    int NeedsReviewCases,
    int MatchCases,
    IReadOnlyCollection<CaseSummaryDto> RecentCases);
