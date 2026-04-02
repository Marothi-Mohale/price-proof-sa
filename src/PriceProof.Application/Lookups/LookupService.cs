using Microsoft.EntityFrameworkCore;
using PriceProof.Application.Abstractions.Persistence;
using PriceProof.Application.Abstractions.Services;

namespace PriceProof.Application.Lookups;

internal sealed class LookupService : ILookupService
{
    private readonly IApplicationDbContext _dbContext;

    public LookupService(IApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<BootstrapLookupsDto> GetBootstrapAsync(CancellationToken cancellationToken)
    {
        var merchants = await _dbContext.Merchants
            .AsNoTracking()
            .Include(entity => entity.Branches)
            .OrderBy(entity => entity.Name)
            .ToListAsync(cancellationToken);

        return new BootstrapLookupsDto(
            merchants.Select(entity => new LookupMerchantDto(
                    entity.Id,
                    entity.Name,
                    entity.Category,
                    entity.WebsiteUrl,
                    entity.Branches
                        .OrderBy(branch => branch.Name)
                        .Select(branch => new LookupBranchDto(
                            branch.Id,
                            branch.MerchantId,
                            branch.Name,
                            branch.Code,
                            branch.AddressLine1,
                            branch.AddressLine2,
                            branch.City,
                            branch.Province,
                            branch.PostalCode))
                        .ToArray()))
                .ToArray());
    }
}
