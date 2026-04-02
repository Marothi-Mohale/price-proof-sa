using Microsoft.EntityFrameworkCore;
using PriceProof.Application.Abstractions.Diagnostics;
using PriceProof.Application.Abstractions.Persistence;
using PriceProof.Application.Abstractions.Services;
using PriceProof.Application.Common;
using PriceProof.Application.Common.Exceptions;
using PriceProof.Application.Common.Models;
using PriceProof.Domain.Entities;
using PriceProof.Domain.Services;

namespace PriceProof.Application.Cases;

internal sealed class CaseService : ICaseService
{
    private readonly IAuditLogWriter _auditLogWriter;
    private readonly IApplicationDbContext _dbContext;
    private readonly IDiscrepancyDetectionEngine _discrepancyDetectionEngine;
    private readonly IRiskService _riskService;

    public CaseService(
        IApplicationDbContext dbContext,
        IDiscrepancyDetectionEngine discrepancyDetectionEngine,
        IRiskService riskService,
        IAuditLogWriter auditLogWriter)
    {
        _dbContext = dbContext;
        _discrepancyDetectionEngine = discrepancyDetectionEngine;
        _riskService = riskService;
        _auditLogWriter = auditLogWriter;
    }

    public async Task<CaseDetailDto> CreateAsync(CreateCaseRequest request, CancellationToken cancellationToken)
    {
        request = request with
        {
            BasketDescription = InputSanitizer.SanitizeRequiredSingleLine(request.BasketDescription, 500),
            CurrencyCode = InputSanitizer.SanitizeCurrencyCode(request.CurrencyCode),
            CustomerReference = InputSanitizer.SanitizeSingleLine(request.CustomerReference, 64),
            Notes = InputSanitizer.SanitizeMultiline(request.Notes, 2000)
        };

        var user = await _dbContext.Users
            .SingleOrDefaultAsync(entity => entity.Id == request.ReportedByUserId, cancellationToken);

        if (user is null)
        {
            throw new NotFoundException($"User '{request.ReportedByUserId}' was not found.");
        }

        var merchant = await _dbContext.Merchants
            .SingleOrDefaultAsync(entity => entity.Id == request.MerchantId, cancellationToken);

        if (merchant is null)
        {
            throw new NotFoundException($"Merchant '{request.MerchantId}' was not found.");
        }

        if (request.BranchId.HasValue)
        {
            var branchExists = await _dbContext.Branches.AnyAsync(
                entity => entity.Id == request.BranchId.Value && entity.MerchantId == request.MerchantId,
                cancellationToken);

            if (!branchExists)
            {
                throw new NotFoundException($"Branch '{request.BranchId.Value}' was not found for merchant '{request.MerchantId}'.");
            }
        }

        var discrepancyCase = DiscrepancyCase.Create(
            request.ReportedByUserId,
            request.MerchantId,
            request.BranchId,
            request.BasketDescription,
            request.IncidentAtUtc,
            request.CurrencyCode,
            request.CustomerReference,
            request.Notes);

        _dbContext.DiscrepancyCases.Add(discrepancyCase);
        _auditLogWriter.Write(
            nameof(DiscrepancyCase),
            "CaseCreated",
            request,
            DateTimeOffset.UtcNow,
            request.ReportedByUserId,
            discrepancyCase.Id);

        await _dbContext.SaveChangesAsync(cancellationToken);

        discrepancyCase = await LoadCaseAsync(discrepancyCase.Id, cancellationToken);
        return discrepancyCase.ToDetailDto();
    }

    public async Task<CaseDetailDto> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var discrepancyCase = await LoadCaseOrDefaultAsync(id, cancellationToken);

        if (discrepancyCase is null)
        {
            throw new NotFoundException($"Case '{id}' was not found.");
        }

        return discrepancyCase.ToDetailDto();
    }

    public async Task<PagedResult<CaseSummaryDto>> ListAsync(GetCasesQuery query, CancellationToken cancellationToken)
    {
        var take = Math.Clamp(query.Take, 1, 100);
        var skip = Math.Max(0, query.Skip);

        var casesQuery = _dbContext.DiscrepancyCases
            .AsNoTracking()
            .Include(entity => entity.Merchant)
            .Include(entity => entity.Branch)
            .AsQueryable();

        if (query.MerchantId.HasValue)
        {
            casesQuery = casesQuery.Where(entity => entity.MerchantId == query.MerchantId.Value);
        }

        if (query.ReportedByUserId.HasValue)
        {
            casesQuery = casesQuery.Where(entity => entity.ReportedByUserId == query.ReportedByUserId.Value);
        }

        if (query.Classification.HasValue)
        {
            casesQuery = casesQuery.Where(entity => entity.Classification == query.Classification.Value);
        }

        var totalCount = await casesQuery.CountAsync(cancellationToken);

        var entities = await casesQuery
            .OrderByDescending(entity => entity.UpdatedUtc)
            .ThenByDescending(entity => entity.CreatedUtc)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);

        var items = entities.Select(entity => entity.ToSummaryDto()).ToArray();

        return new PagedResult<CaseSummaryDto>(items, totalCount, skip, take);
    }

    public async Task<CaseAnalysisDto> AnalyzeAsync(Guid id, AnalyzeCaseRequest request, CancellationToken cancellationToken)
    {
        request = request with
        {
            EvidenceText = InputSanitizer.SanitizeMultiline(request.EvidenceText, 2000)
        };

        var discrepancyCase = await _dbContext.DiscrepancyCases
            .Include(entity => entity.PriceCaptures)
            .Include(entity => entity.PaymentRecords)
                .ThenInclude(record => record.ReceiptRecord)
            .SingleOrDefaultAsync(entity => entity.Id == id, cancellationToken);

        if (discrepancyCase is null)
        {
            throw new NotFoundException($"Case '{id}' was not found.");
        }

        if (!discrepancyCase.LatestQuotedAmount.HasValue || !discrepancyCase.LatestPaidAmount.HasValue)
        {
            throw new ConflictException("A case needs both a quoted amount and a charged amount before analysis can run.");
        }

        var analysis = _discrepancyDetectionEngine.Analyze(new DiscrepancyAnalysisInput(
            discrepancyCase.LatestQuotedAmount.Value,
            discrepancyCase.LatestPaidAmount.Value,
            discrepancyCase.CurrencyCode,
            request.MerchantSaidCardFee,
            request.CashbackPresent,
            request.DeliveryOrServiceFeePresent,
            BuildEvidenceText(discrepancyCase, request.EvidenceText)));

        var now = DateTimeOffset.UtcNow;
        discrepancyCase.ApplyAnalysis(analysis, now);
        await _riskService.RecalculateAsync(discrepancyCase, now, cancellationToken);

        _auditLogWriter.Write(
            nameof(DiscrepancyCase),
            "CaseAnalyzed",
            new
            {
                CaseId = discrepancyCase.Id,
                Request = request,
                Result = new
                {
                    analysis.QuotedAmount,
                    analysis.ChargedAmount,
                    analysis.Difference,
                    analysis.PercentageDifference,
                    Classification = analysis.Classification.ToString(),
                    analysis.Confidence,
                    analysis.Explanation
                }
            },
            now,
            discrepancyCase.ReportedByUserId,
            discrepancyCase.Id);

        await _dbContext.SaveChangesAsync(cancellationToken);

        return new CaseAnalysisDto(
            analysis.QuotedAmount,
            analysis.ChargedAmount,
            analysis.Difference,
            analysis.PercentageDifference,
            analysis.Classification.ToString(),
            analysis.Confidence,
            analysis.Explanation);
    }

    private async Task<DiscrepancyCase> LoadCaseAsync(Guid caseId, CancellationToken cancellationToken)
    {
        return await LoadCaseOrDefaultAsync(caseId, cancellationToken)
               ?? throw new NotFoundException($"Case '{caseId}' was not found.");
    }

    private Task<DiscrepancyCase?> LoadCaseOrDefaultAsync(Guid caseId, CancellationToken cancellationToken)
    {
        return _dbContext.DiscrepancyCases
            .AsNoTracking()
            .Include(entity => entity.ReportedByUser)
            .Include(entity => entity.Merchant)
            .Include(entity => entity.Branch)
            .Include(entity => entity.PriceCaptures)
            .Include(entity => entity.PaymentRecords)
                .ThenInclude(record => record.ReceiptRecord)
            .Include(entity => entity.ComplaintPacks)
            .Include(entity => entity.AuditLogs)
            .SingleOrDefaultAsync(entity => entity.Id == caseId, cancellationToken);
    }

    private static string BuildEvidenceText(DiscrepancyCase discrepancyCase, string? additionalEvidenceText)
    {
        var latestPriceCapture = discrepancyCase.PriceCaptures
            .OrderByDescending(capture => capture.CapturedAtUtc)
            .ThenByDescending(capture => capture.CreatedUtc)
            .FirstOrDefault();

        var latestPayment = discrepancyCase.PaymentRecords
            .OrderByDescending(record => record.PaidAtUtc)
            .ThenByDescending(record => record.CreatedUtc)
            .FirstOrDefault();

        var parts = new[]
        {
            additionalEvidenceText,
            discrepancyCase.Notes,
            latestPriceCapture?.MerchantStatement,
            latestPriceCapture?.Notes,
            latestPayment?.Notes,
            latestPayment?.ReceiptRecord?.MerchantName,
            latestPayment?.ReceiptRecord?.RawText
        };

        return string.Join(
            Environment.NewLine,
            parts.Where(value => !string.IsNullOrWhiteSpace(value)).Select(value => value!.Trim()));
    }
}
