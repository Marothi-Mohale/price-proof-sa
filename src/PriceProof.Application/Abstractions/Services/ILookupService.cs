using PriceProof.Application.Lookups;

namespace PriceProof.Application.Abstractions.Services;

public interface ILookupService
{
    Task<BootstrapLookupsDto> GetBootstrapAsync(CancellationToken cancellationToken);
}
