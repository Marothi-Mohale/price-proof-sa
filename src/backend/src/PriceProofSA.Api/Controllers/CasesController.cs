using Hangfire;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using PriceProofSA.Api.Contracts;
using PriceProofSA.Api.Security;
using PriceProofSA.Application.Cases;
using PriceProofSA.Application.Services;
using PriceProofSA.Infrastructure.BackgroundJobs;

namespace PriceProofSA.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/cases")]
public sealed class CasesController : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyCollection<CaseListItemDto>>> ListAsync(
        [FromServices] CaseService caseService,
        CancellationToken cancellationToken)
    {
        var result = await caseService.ListCasesAsync(User.RequireUserId(), User.IsAdmin(), cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    [EnableRateLimiting("submission")]
    public async Task<ActionResult<CaseDetailDto>> CreateAsync(
        [FromBody] CreateCaseApiRequest request,
        [FromServices] CaseService caseService,
        CancellationToken cancellationToken)
    {
        var result = await caseService.CreateCaseAsync(
            User.RequireUserId(),
            new CreateCaseRequest(
                request.MerchantName,
                request.MerchantCategory,
                request.BranchName,
                request.BranchAddress,
                request.BranchCity,
                request.BranchProvince,
                request.BasketDescription),
            cancellationToken);

        return Ok(result);
    }

    [HttpGet("{caseId:guid}")]
    public async Task<ActionResult<CaseDetailDto>> GetAsync(
        Guid caseId,
        [FromServices] CaseService caseService,
        CancellationToken cancellationToken)
    {
        var result = await caseService.GetCaseAsync(User.RequireUserId(), caseId, User.IsAdmin(), cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost("{caseId:guid}/price-captures/manual")]
    [EnableRateLimiting("submission")]
    public async Task<ActionResult<CaseDetailDto>> AddManualPriceCaptureAsync(
        Guid caseId,
        [FromBody] AddManualPriceCaptureApiRequest request,
        [FromServices] CaseService caseService,
        CancellationToken cancellationToken)
    {
        var result = await caseService.AddManualPriceCaptureAsync(
            User.RequireUserId(),
            caseId,
            new AddManualPriceCaptureRequest(request.Amount, request.QuoteText, request.Notes),
            User.IsAdmin(),
            cancellationToken);

        return Ok(result);
    }

    [HttpPost("{caseId:guid}/price-captures/media")]
    [EnableRateLimiting("submission")]
    [RequestSizeLimit(25 * 1024 * 1024)]
    public async Task<ActionResult<CaseDetailDto>> AddMediaPriceCaptureAsync(
        Guid caseId,
        [FromForm] AddMediaPriceCaptureApiRequest request,
        [FromServices] CaseService caseService,
        CancellationToken cancellationToken)
    {
        if (request.File is null)
        {
            return BadRequest(new { message = "A media file is required." });
        }

        await using var stream = request.File.OpenReadStream();
        var result = await caseService.AddMediaPriceCaptureAsync(
            User.RequireUserId(),
            caseId,
            new AddMediaPriceCaptureRequest(
                request.Mode,
                request.Amount,
                request.QuoteText,
                request.Notes,
                request.File.FileName,
                request.File.ContentType,
                stream),
            User.IsAdmin(),
            cancellationToken);

        return Ok(result);
    }

    [HttpGet("merchant-qr-lock-stub")]
    public async Task<ActionResult<QrQuoteLockStubResponse>> GetMerchantQrStubAsync(
        [FromServices] CaseService caseService,
        CancellationToken cancellationToken)
    {
        return Ok(await caseService.GetQrQuoteLockStubAsync(cancellationToken));
    }

    [HttpPost("{caseId:guid}/payments/manual")]
    [EnableRateLimiting("submission")]
    public async Task<ActionResult<CaseDetailDto>> AddManualPaymentAsync(
        Guid caseId,
        [FromBody] AddManualPaymentApiRequest request,
        [FromServices] CaseService caseService,
        CancellationToken cancellationToken)
    {
        var result = await caseService.AddManualPaymentAsync(
            User.RequireUserId(),
            caseId,
            new AddManualPaymentRequest(request.Amount, request.Mode, request.IsCardPayment, request.Note, request.BankNotificationText),
            User.IsAdmin(),
            cancellationToken);

        return Ok(result);
    }

    [HttpPost("{caseId:guid}/payments/receipt")]
    [EnableRateLimiting("submission")]
    [RequestSizeLimit(25 * 1024 * 1024)]
    public async Task<ActionResult<object>> AddReceiptPaymentAsync(
        Guid caseId,
        [FromForm] AddReceiptPaymentApiRequest request,
        [FromServices] CaseService caseService,
        [FromServices] IBackgroundJobClient backgroundJobClient,
        CancellationToken cancellationToken)
    {
        if (request.File is null)
        {
            return BadRequest(new { message = "A receipt file is required." });
        }

        await using var stream = request.File.OpenReadStream();
        var result = await caseService.AddReceiptPaymentAsync(
            User.RequireUserId(),
            caseId,
            new AddReceiptPaymentRequest(
                request.IsCardPayment,
                request.Note,
                request.EnteredAmount,
                request.File.FileName,
                request.File.ContentType,
                stream),
            User.IsAdmin(),
            cancellationToken);

        backgroundJobClient.Enqueue<ReceiptOcrJob>(job => job.ProcessAsync(result.ReceiptId));
        return Accepted(new { receiptId = result.ReceiptId, @case = result.Case });
    }

    [HttpPost("{caseId:guid}/complaint-pack")]
    public async Task<ActionResult<CaseDetailDto>> GenerateComplaintPackAsync(
        Guid caseId,
        [FromServices] CaseService caseService,
        CancellationToken cancellationToken)
    {
        var result = await caseService.GenerateComplaintPackAsync(User.RequireUserId(), caseId, User.IsAdmin(), cancellationToken);
        return Ok(result);
    }

    [HttpGet("{caseId:guid}/complaint-pack/download")]
    public async Task<IActionResult> DownloadComplaintPackAsync(
        Guid caseId,
        [FromServices] CaseService caseService,
        CancellationToken cancellationToken)
    {
        var file = await caseService.DownloadComplaintPackAsync(User.RequireUserId(), caseId, User.IsAdmin(), cancellationToken);
        return File(file.Content, file.ContentType, file.FileName);
    }
}
