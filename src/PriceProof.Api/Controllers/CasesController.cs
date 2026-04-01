using Microsoft.AspNetCore.Mvc;
using PriceProof.Application.Abstractions.Services;
using PriceProof.Application.Cases;

namespace PriceProof.Api.Controllers;

[ApiController]
[Route("cases")]
public sealed class CasesController : ControllerBase
{
    [HttpPost]
    [ProducesResponseType(typeof(CaseDetailDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<CaseDetailDto>> CreateAsync(
        [FromBody] CreateCaseRequest request,
        [FromServices] ICaseService caseService,
        CancellationToken cancellationToken)
    {
        var result = await caseService.CreateAsync(request, cancellationToken);
        return Created($"/cases/{result.Id}", result);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(CaseDetailDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<CaseDetailDto>> GetByIdAsync(
        Guid id,
        [FromServices] ICaseService caseService,
        CancellationToken cancellationToken)
    {
        var result = await caseService.GetByIdAsync(id, cancellationToken);
        return Ok(result);
    }

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> ListAsync(
        [FromQuery] GetCasesQuery query,
        [FromServices] ICaseService caseService,
        CancellationToken cancellationToken)
    {
        var result = await caseService.ListAsync(query, cancellationToken);
        return Ok(result);
    }

    [HttpPost("{id:guid}/analyze")]
    [ProducesResponseType(typeof(CaseAnalysisDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<CaseAnalysisDto>> AnalyzeAsync(
        Guid id,
        [FromBody] AnalyzeCaseRequest request,
        [FromServices] ICaseService caseService,
        CancellationToken cancellationToken)
    {
        var result = await caseService.AnalyzeAsync(id, request, cancellationToken);
        return Ok(result);
    }
}
