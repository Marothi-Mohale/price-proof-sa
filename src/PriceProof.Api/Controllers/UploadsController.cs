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
using PriceProof.Infrastructure.Persistence;

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
        await RequireAccessiblePathAsync(path, dbContext, currentUserContext, cancellationToken);
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

    private static async Task RequireAccessiblePathAsync(
        string storagePath,
        IApplicationDbContext dbContext,
        ICurrentUserContext currentUserContext,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(storagePath))
        {
            throw new NotFoundException("The requested file could not be found.");
        }

        var caseOwnerUserId = await dbContext.PriceCaptures
            .AsNoTracking()
            .Where(entity => entity.EvidenceStoragePath == storagePath)
            .Select(entity => entity.Case!.ReportedByUserId)
            .Cast<Guid?>()
            .FirstOrDefaultAsync(cancellationToken)
            ?? await dbContext.ReceiptRecords
                .AsNoTracking()
                .Where(entity => entity.StoragePath == storagePath)
                .Select(entity => entity.Case!.ReportedByUserId)
                .Cast<Guid?>()
                .FirstOrDefaultAsync(cancellationToken);

        if ((!caseOwnerUserId.HasValue || caseOwnerUserId.Value == Guid.Empty) && dbContext is AppDbContext appDbContext)
        {
            var uploadedBlobCaseId = await appDbContext.StoredBinaryObjects
                .AsNoTracking()
                .Where(entity => entity.StorageKey == storagePath)
                .Select(entity => entity.CaseId)
                .FirstOrDefaultAsync(cancellationToken);

            if (uploadedBlobCaseId.HasValue && uploadedBlobCaseId.Value != Guid.Empty)
            {
                caseOwnerUserId = await dbContext.DiscrepancyCases
                    .AsNoTracking()
                    .Where(entity => entity.Id == uploadedBlobCaseId.Value)
                    .Select(entity => entity.ReportedByUserId)
                    .Cast<Guid?>()
                    .FirstOrDefaultAsync(cancellationToken);
            }
        }

        if (!caseOwnerUserId.HasValue || caseOwnerUserId.Value == Guid.Empty)
        {
            throw new NotFoundException("The requested file could not be found.");
        }

        CurrentUserGuards.EnsureCanAccessCase(currentUserContext, caseOwnerUserId.Value);
    }
}
