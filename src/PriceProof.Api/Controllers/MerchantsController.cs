using Microsoft.AspNetCore.Mvc;
using PriceProof.Application.Abstractions.Services;
using PriceProof.Application.Merchants;
using PriceProof.Application.Risk;

namespace PriceProof.Api.Controllers;

[ApiController]
[Route("merchants")]
public sealed class MerchantsController : ControllerBase
{
    [HttpGet("{id:guid}/history")]
    [ProducesResponseType(typeof(MerchantHistoryDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<MerchantHistoryDto>> GetHistoryAsync(
        Guid id,
        [FromServices] IMerchantService merchantService,
        CancellationToken cancellationToken)
    {
        var result = await merchantService.GetHistoryAsync(id, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}/risk")]
    [ProducesResponseType(typeof(MerchantRiskDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<MerchantRiskDto>> GetRiskAsync(
        Guid id,
        [FromServices] IRiskService riskService,
        CancellationToken cancellationToken)
    {
        var result = await riskService.GetMerchantRiskAsync(id, cancellationToken);
        return Ok(result);
    }
}
