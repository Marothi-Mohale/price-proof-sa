using Microsoft.AspNetCore.Mvc;
using PriceProof.Application.Abstractions.Services;
using PriceProof.Application.PriceCaptures;

namespace PriceProof.Api.Controllers;

[ApiController]
[Route("price-captures")]
public sealed class PriceCapturesController : ControllerBase
{
    [HttpPost]
    [ProducesResponseType(typeof(PriceCaptureDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<PriceCaptureDto>> CreateAsync(
        [FromBody] CreatePriceCaptureRequest request,
        [FromServices] IPriceCaptureService priceCaptureService,
        CancellationToken cancellationToken)
    {
        var result = await priceCaptureService.CreateAsync(request, cancellationToken);
        return Created($"/price-captures/{result.Id}", result);
    }
}
