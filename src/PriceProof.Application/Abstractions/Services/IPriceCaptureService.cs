using PriceProof.Application.PriceCaptures;

namespace PriceProof.Application.Abstractions.Services;

public interface IPriceCaptureService
{
    Task<PriceCaptureDto> CreateAsync(CreatePriceCaptureRequest request, CancellationToken cancellationToken);
}
