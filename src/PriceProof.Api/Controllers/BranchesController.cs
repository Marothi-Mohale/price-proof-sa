using Microsoft.AspNetCore.Mvc;
using PriceProof.Application.Abstractions.Services;
using PriceProof.Application.Risk;

namespace PriceProof.Api.Controllers;

[ApiController]
[Route("branches")]
public sealed class BranchesController : ControllerBase
{
    [HttpGet("{id:guid}/risk")]
    [ProducesResponseType(typeof(BranchRiskDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<BranchRiskDto>> GetRiskAsync(
        Guid id,
        [FromServices] IRiskService riskService,
        CancellationToken cancellationToken)
    {
        var result = await riskService.GetBranchRiskAsync(id, cancellationToken);
        return Ok(result);
    }
}
