using Microsoft.AspNetCore.Mvc;
using PriceProof.Application.Abstractions.Services;

namespace PriceProof.Api.Controllers;

[ApiController]
[Route("complaint-packs")]
public sealed class ComplaintPacksController : ControllerBase
{
    [HttpGet("{id:guid}/download")]
    public async Task<IActionResult> DownloadAsync(
        Guid id,
        [FromServices] IComplaintPackService complaintPackService,
        CancellationToken cancellationToken)
    {
        var result = await complaintPackService.DownloadAsync(id, cancellationToken);
        return File(result.Content, result.ContentType, result.FileName);
    }
}
