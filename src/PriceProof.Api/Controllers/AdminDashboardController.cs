using Microsoft.AspNetCore.Mvc;
using PriceProof.Application.Abstractions.Services;
using PriceProof.Application.Admin;
using PriceProof.Application.Common.Models;

namespace PriceProof.Api.Controllers;

[ApiController]
[Route("admin/dashboard")]
public sealed class AdminDashboardController : ControllerBase
{
    [HttpGet("summary")]
    [ProducesResponseType(typeof(AdminDashboardSummaryDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<AdminDashboardSummaryDto>> GetSummaryAsync(
        [FromQuery] AdminDashboardFilterQuery query,
        [FromServices] IAdminAccessService adminAccessService,
        [FromServices] IAdminDashboardService adminDashboardService,
        CancellationToken cancellationToken)
    {
        await adminAccessService.RequireAdminAsync(Request.Headers.Authorization, cancellationToken);
        var result = await adminDashboardService.GetSummaryAsync(query, cancellationToken);
        return Ok(result);
    }

    [HttpGet("merchants")]
    [ProducesResponseType(typeof(PagedResult<AdminMerchantRiskRowDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<AdminMerchantRiskRowDto>>> GetMerchantsAsync(
        [FromQuery] AdminDashboardTableQuery query,
        [FromServices] IAdminAccessService adminAccessService,
        [FromServices] IAdminDashboardService adminDashboardService,
        CancellationToken cancellationToken)
    {
        await adminAccessService.RequireAdminAsync(Request.Headers.Authorization, cancellationToken);
        var result = await adminDashboardService.GetTopMerchantsAsync(query, cancellationToken);
        return Ok(result);
    }

    [HttpGet("branches")]
    [ProducesResponseType(typeof(PagedResult<AdminBranchRiskRowDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<AdminBranchRiskRowDto>>> GetBranchesAsync(
        [FromQuery] AdminDashboardTableQuery query,
        [FromServices] IAdminAccessService adminAccessService,
        [FromServices] IAdminDashboardService adminDashboardService,
        CancellationToken cancellationToken)
    {
        await adminAccessService.RequireAdminAsync(Request.Headers.Authorization, cancellationToken);
        var result = await adminDashboardService.GetTopBranchesAsync(query, cancellationToken);
        return Ok(result);
    }

    [HttpGet("recent-uploads")]
    [ProducesResponseType(typeof(PagedResult<RecentUploadDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<RecentUploadDto>>> GetRecentUploadsAsync(
        [FromQuery] AdminDashboardTableQuery query,
        [FromServices] IAdminAccessService adminAccessService,
        [FromServices] IAdminDashboardService adminDashboardService,
        CancellationToken cancellationToken)
    {
        await adminAccessService.RequireAdminAsync(Request.Headers.Authorization, cancellationToken);
        var result = await adminDashboardService.GetRecentUploadsAsync(query, cancellationToken);
        return Ok(result);
    }

    [HttpGet("export/csv")]
    public async Task<IActionResult> ExportCsvAsync(
        [FromQuery] AdminDashboardFilterQuery query,
        [FromServices] IAdminAccessService adminAccessService,
        [FromServices] IAdminDashboardService adminDashboardService,
        CancellationToken cancellationToken)
    {
        await adminAccessService.RequireAdminAsync(Request.Headers.Authorization, cancellationToken);
        var result = await adminDashboardService.ExportCsvAsync(query, cancellationToken);
        return File(result.Content, result.ContentType, result.FileName);
    }
}
