using Microsoft.AspNetCore.Mvc;
using PriceProof.Application.Abstractions.Services;
using PriceProof.Application.Uploads;

namespace PriceProof.Api.Controllers;

[ApiController]
[Route("uploads")]
public sealed class UploadsController : ControllerBase
{
    [HttpPost]
    [RequestFormLimits(MultipartBodyLengthLimit = 20 * 1024 * 1024)]
    [RequestSizeLimit(20 * 1024 * 1024)]
    [ProducesResponseType(typeof(UploadedFileDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<UploadedFileDto>> UploadAsync(
        [FromForm] IFormFile file,
        [FromForm] string category,
        [FromForm] Guid? caseId,
        [FromServices] IFileUploadService fileUploadService,
        CancellationToken cancellationToken)
    {
        var result = await fileUploadService.UploadAsync(file, category, caseId, cancellationToken);
        return Created($"/uploads/content?path={Uri.EscapeDataString(result.StoragePath)}", result);
    }

    [HttpGet("content")]
    public async Task<IActionResult> DownloadAsync(
        [FromQuery] string path,
        [FromServices] IFileUploadService fileUploadService,
        CancellationToken cancellationToken)
    {
        var result = await fileUploadService.DownloadAsync(path, cancellationToken);
        return File(result.Content, result.ContentType, enableRangeProcessing: true);
    }
}
