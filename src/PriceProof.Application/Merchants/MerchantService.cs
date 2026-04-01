using Microsoft.EntityFrameworkCore;
using PriceProof.Application.Abstractions.Persistence;
using PriceProof.Application.Abstractions.Services;
using PriceProof.Application.Cases;
using PriceProof.Application.Common.Exceptions;
using PriceProof.Domain.Enums;

namespace PriceProof.Application.Merchants;

internal sealed class MerchantService : IMerchantService
{
    private readonly IApplicationDbContext _dbContext;

    public MerchantService(IApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<MerchantHistoryDto> GetHistoryAsync(Guid merchantId, CancellationToken cancellationToken)
    {
        var merchant = await _dbContext.Merchants
            .AsNoTracking()
            .SingleOrDefaultAsync(entity => entity.Id == merchantId, cancellationToken);

        if (merchant is null)
        {
            throw new NotFoundException($"Merchant '{merchantId}' was not found.");
        }

        var cases = await _dbContext.DiscrepancyCases
            .AsNoTracking()
            .Include(entity => entity.Merchant)
            .Include(entity => entity.Branch)
            .Where(entity => entity.MerchantId == merchantId)
            .OrderByDescending(entity => entity.UpdatedUtc)
            .ThenByDescending(entity => entity.CreatedUtc)
            .ToListAsync(cancellationToken);

        return new MerchantHistoryDto(
            merchant.Id,
            merchant.Name,
            merchant.Category,
            merchant.WebsiteUrl,
            cases.Count,
            cases.Count(entity => entity.Classification == CaseClassification.PotentialCardSurcharge),
            cases.Count(entity => entity.Classification == CaseClassification.NeedsReview),
            cases.Count(entity => entity.Classification == CaseClassification.Match),
            cases.Take(12).Select(entity => entity.ToSummaryDto()).ToArray());
    }
}
