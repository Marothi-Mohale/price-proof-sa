using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Mvc;
using PriceProof.Application.Abstractions.Services;
using PriceProof.Application.Risk;

namespace PriceProof.Api.Controllers;

[ApiController]
[Route("risk")]
[EnableRateLimiting("admin")]
public sealed class RiskController : ControllerBase
{
    [HttpGet("overview")]
    [ProducesResponseType(typeof(RiskOverviewDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<RiskOverviewDto>> GetOverviewAsync(
        [FromServices] IAdminAccessService adminAccessService,
        [FromServices] IRiskService riskService,
        CancellationToken cancellationToken)
    {
        await adminAccessService.RequireAdminAsync(Request.Headers.Authorization, cancellationToken);
        var result = await riskService.GetOverviewAsync(cancellationToken);
        return Ok(result);
    }
}
