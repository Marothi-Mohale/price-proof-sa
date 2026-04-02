using System.Text;
using Microsoft.EntityFrameworkCore;
using PriceProof.Application.Abstractions.Persistence;
using PriceProof.Application.Abstractions.Services;
using PriceProof.Application.Common.Models;
using PriceProof.Domain.Entities;
using PriceProof.Domain.Enums;
using PriceProof.Domain.Services;

namespace PriceProof.Application.Admin;

internal sealed class AdminDashboardService : IAdminDashboardService
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IRiskScoringEngine _riskScoringEngine;

    public AdminDashboardService(IApplicationDbContext dbContext, IRiskScoringEngine riskScoringEngine)
    {
        _dbContext = dbContext;
        _riskScoringEngine = riskScoringEngine;
    }

    public async Task<AdminDashboardSummaryDto> GetSummaryAsync(AdminDashboardFilterQuery query, CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var filteredCases = await LoadFilteredCasesAsync(query, cancellationToken);
        var merchantRows = BuildMerchantRows(filteredCases, now).Take(5).ToArray();
        var branchRows = BuildBranchRows(filteredCases, now).Take(5).ToArray();

        var ocrMetrics = await LoadOcrMetricsAsync(query, cancellationToken);
        var complaintPackCount = await LoadComplaintPackCountAsync(query, cancellationToken);

        var classificationCounts = Enum.GetValues<CaseClassification>()
            .Select(classification => new ClassificationCountDto(
                classification.ToString(),
                filteredCases.Count(item => item.Classification == classification)))
            .ToArray();

        return new AdminDashboardSummaryDto(
            filteredCases.Count,
            filteredCases.Count(item => item.Status != CaseStatus.Closed),
            complaintPackCount,
            ocrMetrics.SuccessRate,
            ocrMetrics.AttemptCount,
            ocrMetrics.SuccessCount,
            classificationCounts,
            merchantRows,
            branchRows);
    }

    public async Task<PagedResult<AdminMerchantRiskRowDto>> GetTopMerchantsAsync(AdminDashboardTableQuery query, CancellationToken cancellationToken)
    {
        var filteredCases = await LoadFilteredCasesAsync(query, cancellationToken);
        var rows = BuildMerchantRows(filteredCases, DateTimeOffset.UtcNow).ToArray();
        return Page(rows, query.Skip, query.Take);
    }

    public async Task<PagedResult<AdminBranchRiskRowDto>> GetTopBranchesAsync(AdminDashboardTableQuery query, CancellationToken cancellationToken)
    {
        var filteredCases = await LoadFilteredCasesAsync(query, cancellationToken);
        var rows = BuildBranchRows(filteredCases, DateTimeOffset.UtcNow).ToArray();
        return Page(rows, query.Skip, query.Take);
    }

    public async Task<PagedResult<RecentUploadDto>> GetRecentUploadsAsync(AdminDashboardTableQuery query, CancellationToken cancellationToken)
    {
        var priceCaptures = await ApplyFilters(_dbContext.PriceCaptures.AsNoTracking(), query)
            .Select(entity => new RecentUploadDto(
                "Price evidence",
                entity.CaseId,
                entity.Case!.MerchantId,
                entity.Case.Merchant!.Name,
                entity.Case.BranchId,
                entity.Case.Branch != null ? entity.Case.Branch.Name : null,
                entity.Case.Branch != null ? entity.Case.Branch.City : null,
                entity.Case.Branch != null ? entity.Case.Branch.Province : null,
                entity.FileName,
                entity.EvidenceType.ToString(),
                entity.EvidenceStoragePath,
                entity.CapturedByUser!.DisplayName,
                entity.CapturedAtUtc))
            .ToListAsync(cancellationToken);

        var receiptRecords = await ApplyFilters(_dbContext.ReceiptRecords.AsNoTracking(), query)
            .Select(entity => new RecentUploadDto(
                "Receipt evidence",
                entity.CaseId,
                entity.Case!.MerchantId,
                entity.Case.Merchant!.Name,
                entity.Case.BranchId,
                entity.Case.Branch != null ? entity.Case.Branch.Name : null,
                entity.Case.Branch != null ? entity.Case.Branch.City : null,
                entity.Case.Branch != null ? entity.Case.Branch.Province : null,
                entity.FileName,
                entity.EvidenceType.ToString(),
                entity.StoragePath,
                entity.UploadedByUser!.DisplayName,
                entity.UploadedAtUtc))
            .ToListAsync(cancellationToken);

        var rows = priceCaptures
            .Concat(receiptRecords)
            .OrderByDescending(item => item.UploadedUtc)
            .ThenByDescending(item => item.CaseId)
            .ToArray();

        return Page(rows, query.Skip, query.Take);
    }

    public async Task<AdminDashboardCsvExportDto> ExportCsvAsync(AdminDashboardFilterQuery query, CancellationToken cancellationToken)
    {
        var summary = await GetSummaryAsync(query, cancellationToken);
        var merchants = await GetTopMerchantsAsync(new AdminDashboardTableQuery
        {
            FromDate = query.FromDate,
            ToDate = query.ToDate,
            Province = query.Province,
            City = query.City,
            Skip = 0,
            Take = 200
        }, cancellationToken);
        var branches = await GetTopBranchesAsync(new AdminDashboardTableQuery
        {
            FromDate = query.FromDate,
            ToDate = query.ToDate,
            Province = query.Province,
            City = query.City,
            Skip = 0,
            Take = 200
        }, cancellationToken);
        var uploads = await GetRecentUploadsAsync(new AdminDashboardTableQuery
        {
            FromDate = query.FromDate,
            ToDate = query.ToDate,
            Province = query.Province,
            City = query.City,
            Skip = 0,
            Take = 500
        }, cancellationToken);

        var builder = new StringBuilder();
        builder.AppendLine("Section,Key,Value");
        builder.AppendLine($"Summary,GeneratedUtc,{Escape(DateTimeOffset.UtcNow.ToString("O"))}");
        builder.AppendLine($"Summary,FromDate,{Escape(query.FromDate?.ToString("yyyy-MM-dd"))}");
        builder.AppendLine($"Summary,ToDate,{Escape(query.ToDate?.ToString("yyyy-MM-dd"))}");
        builder.AppendLine($"Summary,Province,{Escape(query.Province)}");
        builder.AppendLine($"Summary,City,{Escape(query.City)}");
        builder.AppendLine($"Summary,TotalCases,{summary.TotalCases}");
        builder.AppendLine($"Summary,UnresolvedCases,{summary.UnresolvedCases}");
        builder.AppendLine($"Summary,ComplaintPackGenerationCount,{summary.ComplaintPackGenerationCount}");
        builder.AppendLine($"Summary,OcrSuccessRate,{summary.OcrSuccessRate}");
        builder.AppendLine($"Summary,OcrAttemptCount,{summary.OcrAttemptCount}");
        builder.AppendLine($"Summary,OcrSuccessCount,{summary.OcrSuccessCount}");
        builder.AppendLine();

        builder.AppendLine("CasesByClassification,Classification,Count");
        foreach (var item in summary.CasesByClassification)
        {
            builder.AppendLine($"CasesByClassification,{Escape(item.Classification)},{item.Count}");
        }

        builder.AppendLine();
        builder.AppendLine("TopMerchants,MerchantName,Category,RiskScore,RiskLabel,TotalCases,AnalyzedCases,LikelyCardSurchargeCases");
        foreach (var item in merchants.Items)
        {
            builder.AppendLine(
                $"TopMerchants,{Escape(item.MerchantName)},{Escape(item.Category)},{item.RiskScore},{Escape(item.RiskLabel)},{item.TotalCases},{item.AnalyzedCases},{item.LikelyCardSurchargeCases}");
        }

        builder.AppendLine();
        builder.AppendLine("TopBranches,BranchName,MerchantName,City,Province,RiskScore,RiskLabel,TotalCases,AnalyzedCases,LikelyCardSurchargeCases");
        foreach (var item in branches.Items)
        {
            builder.AppendLine(
                $"TopBranches,{Escape(item.BranchName)},{Escape(item.MerchantName)},{Escape(item.City)},{Escape(item.Province)},{item.RiskScore},{Escape(item.RiskLabel)},{item.TotalCases},{item.AnalyzedCases},{item.LikelyCardSurchargeCases}");
        }

        builder.AppendLine();
        builder.AppendLine("RecentUploads,UploadKind,MerchantName,BranchName,City,Province,FileName,EvidenceType,UploadedBy,UploadedUtc");
        foreach (var item in uploads.Items)
        {
            builder.AppendLine(
                $"RecentUploads,{Escape(item.UploadKind)},{Escape(item.MerchantName)},{Escape(item.BranchName)},{Escape(item.City)},{Escape(item.Province)},{Escape(item.FileName)},{Escape(item.EvidenceType)},{Escape(item.UploadedBy)},{Escape(item.UploadedUtc.ToString("O"))}");
        }

        return new AdminDashboardCsvExportDto(
            $"priceproof-admin-report-{DateTime.UtcNow:yyyyMMddHHmmss}.csv",
            "text/csv",
            Encoding.UTF8.GetBytes(builder.ToString()));
    }

    private async Task<List<DashboardCaseRow>> LoadFilteredCasesAsync(AdminDashboardFilterQuery query, CancellationToken cancellationToken)
    {
        return await ApplyFilters(_dbContext.DiscrepancyCases.AsNoTracking(), query)
            .Select(entity => new DashboardCaseRow(
                entity.Id,
                entity.MerchantId,
                entity.Merchant!.Name,
                entity.Merchant.Category,
                entity.BranchId,
                entity.Branch != null ? entity.Branch.Name : null,
                entity.Branch != null ? entity.Branch.City : null,
                entity.Branch != null ? entity.Branch.Province : null,
                entity.Classification,
                entity.Status,
                entity.AnalysisClassification,
                entity.AnalysisConfidence,
                entity.DifferenceAmount,
                entity.IncidentAtUtc,
                entity.AnalysisUpdatedUtc))
            .ToListAsync(cancellationToken);
    }

    private async Task<(int AttemptCount, int SuccessCount, decimal SuccessRate)> LoadOcrMetricsAsync(
        AdminDashboardFilterQuery query,
        CancellationToken cancellationToken)
    {
        var receipts = await ApplyFilters(_dbContext.ReceiptRecords.AsNoTracking(), query)
            .Select(entity => new
            {
                entity.OcrProcessedUtc,
                entity.ParsedTotalAmount,
                entity.MerchantName,
                entity.TransactionAtUtc
            })
            .ToListAsync(cancellationToken);

        var attempts = receipts.Count(item => item.OcrProcessedUtc.HasValue);
        var successes = receipts.Count(item =>
            item.OcrProcessedUtc.HasValue &&
            (item.ParsedTotalAmount.HasValue || !string.IsNullOrWhiteSpace(item.MerchantName) || item.TransactionAtUtc.HasValue));
        var successRate = attempts == 0
            ? 0m
            : decimal.Round((decimal)successes / attempts * 100m, 2, MidpointRounding.AwayFromZero);

        return (attempts, successes, successRate);
    }

    private async Task<int> LoadComplaintPackCountAsync(AdminDashboardFilterQuery query, CancellationToken cancellationToken)
    {
        return await ApplyFilters(_dbContext.ComplaintPacks.AsNoTracking(), query)
            .CountAsync(cancellationToken);
    }

    private IEnumerable<AdminMerchantRiskRowDto> BuildMerchantRows(IEnumerable<DashboardCaseRow> filteredCases, DateTimeOffset now)
    {
        return filteredCases
            .GroupBy(item => new { item.MerchantId, item.MerchantName, item.Category })
            .Select(group =>
            {
                var score = _riskScoringEngine.Calculate(group.Select(ToRiskSignal), now);
                return new AdminMerchantRiskRowDto(
                    group.Key.MerchantId,
                    group.Key.MerchantName,
                    group.Key.Category,
                    score.Score,
                    score.Label.ToString(),
                    score.TotalCases,
                    score.AnalyzedCases,
                    score.LikelyCardSurchargeCases);
            })
            .OrderByDescending(item => item.RiskScore)
            .ThenByDescending(item => item.TotalCases)
            .ThenBy(item => item.MerchantName);
    }

    private IEnumerable<AdminBranchRiskRowDto> BuildBranchRows(IEnumerable<DashboardCaseRow> filteredCases, DateTimeOffset now)
    {
        return filteredCases
            .Where(item => item.BranchId.HasValue)
            .GroupBy(item => new
            {
                BranchId = item.BranchId!.Value,
                item.MerchantId,
                item.BranchName,
                item.MerchantName,
                item.City,
                item.Province
            })
            .Select(group =>
            {
                var score = _riskScoringEngine.Calculate(group.Select(ToRiskSignal), now);
                return new AdminBranchRiskRowDto(
                    group.Key.BranchId,
                    group.Key.MerchantId,
                    group.Key.BranchName ?? "Unknown branch",
                    group.Key.MerchantName,
                    group.Key.City ?? "Unknown city",
                    group.Key.Province ?? "Unknown province",
                    score.Score,
                    score.Label.ToString(),
                    score.TotalCases,
                    score.AnalyzedCases,
                    score.LikelyCardSurchargeCases);
            })
            .OrderByDescending(item => item.RiskScore)
            .ThenByDescending(item => item.TotalCases)
            .ThenBy(item => item.BranchName);
    }

    private static RiskCaseSignal ToRiskSignal(DashboardCaseRow row)
    {
        return new RiskCaseSignal(
            row.CaseId,
            row.AnalysisClassification,
            row.AnalysisConfidence,
            row.DifferenceAmount,
            row.IncidentAtUtc,
            row.AnalysisUpdatedUtc);
    }

    private static IQueryable<DiscrepancyCase> ApplyFilters(IQueryable<DiscrepancyCase> query, AdminDashboardFilterQuery filter)
    {
        var range = GetDateRange(filter);

        if (!string.IsNullOrWhiteSpace(filter.Province))
        {
            var province = filter.Province.Trim();
            query = query.Where(entity => entity.Branch != null && entity.Branch.Province == province);
        }

        if (!string.IsNullOrWhiteSpace(filter.City))
        {
            var city = filter.City.Trim();
            query = query.Where(entity => entity.Branch != null && entity.Branch.City == city);
        }

        if (range.StartUtc.HasValue)
        {
            query = query.Where(entity => entity.IncidentAtUtc >= range.StartUtc.Value);
        }

        if (range.EndUtcExclusive.HasValue)
        {
            query = query.Where(entity => entity.IncidentAtUtc < range.EndUtcExclusive.Value);
        }

        return query;
    }

    private static IQueryable<PriceCapture> ApplyFilters(IQueryable<PriceCapture> query, AdminDashboardFilterQuery filter)
    {
        var range = GetDateRange(filter);

        if (!string.IsNullOrWhiteSpace(filter.Province))
        {
            var province = filter.Province.Trim();
            query = query.Where(entity => entity.Case!.Branch != null && entity.Case.Branch.Province == province);
        }

        if (!string.IsNullOrWhiteSpace(filter.City))
        {
            var city = filter.City.Trim();
            query = query.Where(entity => entity.Case!.Branch != null && entity.Case.Branch.City == city);
        }

        if (range.StartUtc.HasValue)
        {
            query = query.Where(entity => entity.CapturedAtUtc >= range.StartUtc.Value);
        }

        if (range.EndUtcExclusive.HasValue)
        {
            query = query.Where(entity => entity.CapturedAtUtc < range.EndUtcExclusive.Value);
        }

        return query;
    }

    private static IQueryable<ReceiptRecord> ApplyFilters(IQueryable<ReceiptRecord> query, AdminDashboardFilterQuery filter)
    {
        var range = GetDateRange(filter);

        if (!string.IsNullOrWhiteSpace(filter.Province))
        {
            var province = filter.Province.Trim();
            query = query.Where(entity => entity.Case!.Branch != null && entity.Case.Branch.Province == province);
        }

        if (!string.IsNullOrWhiteSpace(filter.City))
        {
            var city = filter.City.Trim();
            query = query.Where(entity => entity.Case!.Branch != null && entity.Case.Branch.City == city);
        }

        if (range.StartUtc.HasValue)
        {
            query = query.Where(entity => entity.UploadedAtUtc >= range.StartUtc.Value);
        }

        if (range.EndUtcExclusive.HasValue)
        {
            query = query.Where(entity => entity.UploadedAtUtc < range.EndUtcExclusive.Value);
        }

        return query;
    }

    private static IQueryable<ComplaintPack> ApplyFilters(IQueryable<ComplaintPack> query, AdminDashboardFilterQuery filter)
    {
        var range = GetDateRange(filter);

        if (!string.IsNullOrWhiteSpace(filter.Province))
        {
            var province = filter.Province.Trim();
            query = query.Where(entity => entity.Case!.Branch != null && entity.Case.Branch.Province == province);
        }

        if (!string.IsNullOrWhiteSpace(filter.City))
        {
            var city = filter.City.Trim();
            query = query.Where(entity => entity.Case!.Branch != null && entity.Case.Branch.City == city);
        }

        if (range.StartUtc.HasValue)
        {
            query = query.Where(entity => entity.GeneratedAtUtc >= range.StartUtc.Value);
        }

        if (range.EndUtcExclusive.HasValue)
        {
            query = query.Where(entity => entity.GeneratedAtUtc < range.EndUtcExclusive.Value);
        }

        return query;
    }

    private static DashboardDateRange GetDateRange(AdminDashboardFilterQuery query)
    {
        DateTimeOffset? startUtc = query.FromDate.HasValue
            ? new DateTimeOffset(query.FromDate.Value.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc))
            : null;
        DateTimeOffset? endUtcExclusive = query.ToDate.HasValue
            ? new DateTimeOffset(query.ToDate.Value.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc))
            : null;

        return new DashboardDateRange(startUtc, endUtcExclusive);
    }

    private static PagedResult<T> Page<T>(IReadOnlyCollection<T> items, int skip, int take)
    {
        var normalizedSkip = Math.Max(0, skip);
        var normalizedTake = Math.Clamp(take <= 0 ? 10 : take, 1, 100);
        var pageItems = items.Skip(normalizedSkip).Take(normalizedTake).ToArray();
        return new PagedResult<T>(pageItems, items.Count, normalizedSkip, normalizedTake);
    }

    private static string Escape(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var normalized = value.Replace("\"", "\"\"");
        return $"\"{normalized}\"";
    }

    private sealed record DashboardCaseRow(
        Guid CaseId,
        Guid MerchantId,
        string MerchantName,
        string? Category,
        Guid? BranchId,
        string? BranchName,
        string? City,
        string? Province,
        CaseClassification Classification,
        CaseStatus Status,
        DiscrepancyAnalysisClassification? AnalysisClassification,
        decimal? AnalysisConfidence,
        decimal? DifferenceAmount,
        DateTimeOffset IncidentAtUtc,
        DateTimeOffset? AnalysisUpdatedUtc);

    private sealed record DashboardDateRange(
        DateTimeOffset? StartUtc,
        DateTimeOffset? EndUtcExclusive);
}
