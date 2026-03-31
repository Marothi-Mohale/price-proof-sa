using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PriceProofSA.Application.Merchants;
using PriceProofSA.Application.Services;

namespace PriceProofSA.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/merchants")]
public sealed class MerchantsController : ControllerBase
{
    [HttpGet("{merchantId:guid}/history")]
    public async Task<ActionResult<MerchantHistoryDto>> GetHistoryAsync(
        Guid merchantId,
        [FromServices] MerchantService merchantService,
        CancellationToken cancellationToken)
    {
        var history = await merchantService.GetMerchantHistoryAsync(merchantId, cancellationToken);
        return history is null ? NotFound() : Ok(history);
    }
}
