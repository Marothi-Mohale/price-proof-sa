using PriceProof.Application.Admin;
using PriceProof.Application.Common.Models;

namespace PriceProof.Application.Abstractions.Services;

public interface IAdminDashboardService
{
    Task<AdminDashboardSummaryDto> GetSummaryAsync(AdminDashboardFilterQuery query, CancellationToken cancellationToken);

    Task<PagedResult<AdminMerchantRiskRowDto>> GetTopMerchantsAsync(AdminDashboardTableQuery query, CancellationToken cancellationToken);

    Task<PagedResult<AdminBranchRiskRowDto>> GetTopBranchesAsync(AdminDashboardTableQuery query, CancellationToken cancellationToken);

    Task<PagedResult<RecentUploadDto>> GetRecentUploadsAsync(AdminDashboardTableQuery query, CancellationToken cancellationToken);

    Task<AdminDashboardCsvExportDto> ExportCsvAsync(AdminDashboardFilterQuery query, CancellationToken cancellationToken);
}
