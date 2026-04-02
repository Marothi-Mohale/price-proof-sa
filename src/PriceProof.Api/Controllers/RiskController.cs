using Microsoft.AspNetCore.Mvc;
using PriceProof.Application.Abstractions.Services;
using PriceProof.Application.Risk;

namespace PriceProof.Api.Controllers;

[ApiController]
[Route("risk")]
public sealed class RiskController : ControllerBase
{
    [HttpGet("overview")]
    [ProducesResponseType(typeof(RiskOverviewDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<RiskOverviewDto>> GetOverviewAsync(
        [FromQuery] Guid requestedByUserId,
        [FromServices] IRiskService riskService,
        CancellationToken cancellationToken)
    {
        var result = await riskService.GetOverviewAsync(requestedByUserId, cancellationToken);
        return Ok(result);
    }
}
