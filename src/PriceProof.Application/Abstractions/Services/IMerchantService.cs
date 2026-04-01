using PriceProof.Application.Merchants;

namespace PriceProof.Application.Abstractions.Services;

public interface IMerchantService
{
    Task<MerchantHistoryDto> GetHistoryAsync(Guid merchantId, CancellationToken cancellationToken);
}
