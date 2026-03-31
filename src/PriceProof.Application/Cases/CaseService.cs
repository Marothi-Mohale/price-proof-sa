using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using PriceProof.Application.Abstractions.Persistence;
using PriceProof.Application.Abstractions.Services;
using PriceProof.Application.Common.Exceptions;
using PriceProof.Application.Common.Models;
using PriceProof.Domain.Entities;

namespace PriceProof.Application.Cases;

internal sealed class CaseService : ICaseService
{
    private static readonly JsonSerializerOptions AuditJsonOptions = new(JsonSerializerDefaults.Web);

    private readonly IApplicationDbContext _dbContext;

    public CaseService(IApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<CaseDetailDto> CreateAsync(CreateCaseRequest request, CancellationToken cancellationToken)
    {
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
        _dbContext.AuditLogs.Add(AuditLog.Create(
            nameof(DiscrepancyCase),
            "CaseCreated",
            JsonSerializer.Serialize(request, AuditJsonOptions),
            Guid.NewGuid().ToString("N"),
            DateTimeOffset.UtcNow,
            request.ReportedByUserId,
            discrepancyCase.Id));

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
}
