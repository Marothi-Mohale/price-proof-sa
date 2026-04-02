using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using PriceProof.Application.Abstractions.Diagnostics;
using PriceProof.Application.Abstractions.Persistence;
using PriceProof.Application.Abstractions.Security;
using PriceProof.Application.Abstractions.Services;
using PriceProof.Application.Common;
using PriceProof.Application.Common.Exceptions;
using PriceProof.Application.Uploads;

namespace PriceProof.Api.Controllers;

[ApiController]
[Route("uploads")]
public sealed class UploadsController : ControllerBase
{
    [HttpPost]
    [EnableRateLimiting("uploads")]
    [RequestFormLimits(MultipartBodyLengthLimit = 20 * 1024 * 1024)]
    [RequestSizeLimit(20 * 1024 * 1024)]
    [ProducesResponseType(typeof(UploadedFileDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<UploadedFileDto>> UploadAsync(
        [FromForm] IFormFile file,
        [FromForm] string category,
        [FromForm] Guid? caseId,
        [FromServices] IApplicationDbContext dbContext,
        [FromServices] ICurrentUserContext currentUserContext,
        [FromServices] IFileUploadService fileUploadService,
        [FromServices] IAuditLogWriter auditLogWriter,
        CancellationToken cancellationToken)
    {
        var currentUserId = CurrentUserGuards.RequireAuthenticatedUserId(currentUserContext);
        await RequireAccessibleCaseAsync(caseId, dbContext, currentUserContext, cancellationToken);
        var result = await fileUploadService.UploadAsync(file, category, caseId, cancellationToken);
        await auditLogWriter.WriteAndSaveAsync(
            "FileUpload",
            "UploadCreated",
            new
            {
                result.FileName,
                result.ContentType,
                result.StoragePath,
                result.ContentHash,
                result.SizeBytes,
                Category = category
            },
            DateTimeOffset.UtcNow,
            actorUserId: currentUserId,
            caseId: caseId,
            cancellationToken: cancellationToken);
        return Created($"/uploads/content?path={Uri.EscapeDataString(result.StoragePath)}", result);
    }

    [HttpGet("content")]
    public async Task<IActionResult> DownloadAsync(
        [FromQuery] string path,
        [FromServices] IApplicationDbContext dbContext,
        [FromServices] ICurrentUserContext currentUserContext,
        [FromServices] IFileUploadService fileUploadService,
        CancellationToken cancellationToken)
    {
        var caseId = ExtractCaseIdFromStoragePath(path);
        await RequireAccessibleCaseAsync(caseId, dbContext, currentUserContext, cancellationToken);
        var result = await fileUploadService.DownloadAsync(path, cancellationToken);
        return File(result.Content, result.ContentType, enableRangeProcessing: true);
    }

    private static async Task<Guid> RequireAccessibleCaseAsync(
        Guid? caseId,
        IApplicationDbContext dbContext,
        ICurrentUserContext currentUserContext,
        CancellationToken cancellationToken)
    {
        if (!caseId.HasValue || caseId.Value == Guid.Empty)
        {
            throw new BadRequestException("Uploads must be associated with a case.");
        }

        var caseOwnerUserId = await dbContext.DiscrepancyCases
            .AsNoTracking()
            .Where(entity => entity.Id == caseId.Value)
            .Select(entity => entity.ReportedByUserId)
            .SingleOrDefaultAsync(cancellationToken);

        if (caseOwnerUserId == Guid.Empty)
        {
            throw new NotFoundException($"Case '{caseId.Value}' was not found.");
        }

        CurrentUserGuards.EnsureCanAccessCase(currentUserContext, caseOwnerUserId);
        return caseId.Value;
    }

    private static Guid? ExtractCaseIdFromStoragePath(string storagePath)
    {
        var normalizedPath = storagePath
            .Replace('\\', '/')
            .Trim();

        var segments = normalizedPath
            .Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (segments.Length < 4 || !segments[0].Equals("uploads", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        return Guid.TryParse(segments[2], out var caseId) ? caseId : null;
    }
}
