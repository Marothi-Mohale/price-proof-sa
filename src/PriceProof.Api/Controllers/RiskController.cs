using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Mvc;
using PriceProof.Application.Abstractions.Services;
using PriceProof.Application.Risk;

namespace PriceProof.Api.Controllers;

[ApiController]
[Route("risk")]
[EnableRateLimiting("admin")]
[Authorize(Policy = "Admin")]
public sealed class RiskController : ControllerBase
{
    [HttpGet("overview")]
    [ProducesResponseType(typeof(RiskOverviewDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<RiskOverviewDto>> GetOverviewAsync(
        [FromServices] IRiskService riskService,
        CancellationToken cancellationToken)
    {
        var result = await riskService.GetOverviewAsync(cancellationToken);
        return Ok(result);
    }
}
