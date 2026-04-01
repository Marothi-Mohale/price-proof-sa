using Microsoft.AspNetCore.Mvc;
using PriceProof.Application.Abstractions.Services;
using PriceProof.Application.Lookups;

namespace PriceProof.Api.Controllers;

[ApiController]
[Route("lookups")]
public sealed class LookupsController : ControllerBase
{
    [HttpGet("bootstrap")]
    [ProducesResponseType(typeof(BootstrapLookupsDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<BootstrapLookupsDto>> GetBootstrapAsync(
        [FromServices] ILookupService lookupService,
        CancellationToken cancellationToken)
    {
        var result = await lookupService.GetBootstrapAsync(cancellationToken);
        return Ok(result);
    }
}
