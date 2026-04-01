using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using PriceProof.Application.Abstractions.Persistence;
using PriceProof.Application.Abstractions.Services;
using PriceProof.Application.Common.Exceptions;
using PriceProof.Domain.Entities;

namespace PriceProof.Application.PriceCaptures;

internal sealed class PriceCaptureService : IPriceCaptureService
{
    private readonly IApplicationDbContext _dbContext;

    public PriceCaptureService(IApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<PriceCaptureDto> CreateAsync(CreatePriceCaptureRequest request, CancellationToken cancellationToken)
    {
        var discrepancyCase = await _dbContext.DiscrepancyCases
            .Include(entity => entity.PaymentRecords)
                .ThenInclude(record => record.ReceiptRecord)
            .SingleOrDefaultAsync(entity => entity.Id == request.CaseId, cancellationToken);

        if (discrepancyCase is null)
        {
            throw new NotFoundException($"Case '{request.CaseId}' was not found.");
        }

        var userExists = await _dbContext.Users
            .AnyAsync(entity => entity.Id == request.CapturedByUserId, cancellationToken);

        if (!userExists)
        {
            throw new NotFoundException($"User '{request.CapturedByUserId}' was not found.");
        }

        var capture = PriceCapture.Create(
            request.CaseId,
            request.CapturedByUserId,
            request.CaptureType,
            request.EvidenceType,
            request.QuotedAmount,
            request.CurrencyCode,
            request.FileName,
            request.EvidenceStoragePath,
            request.CapturedAtUtc,
            request.ContentType,
            request.EvidenceHash,
            request.MerchantStatement,
            request.Notes);

        var now = DateTimeOffset.UtcNow;
        discrepancyCase.AddPriceCapture(capture, now);
        _dbContext.PriceCaptures.Add(capture);
        _dbContext.AuditLogs.Add(AuditLog.Create(
            nameof(PriceCapture),
            "PriceCaptureCreated",
            JsonSerializer.Serialize(request),
            Guid.NewGuid().ToString("N"),
            now,
            request.CapturedByUserId,
            request.CaseId));

        await _dbContext.SaveChangesAsync(cancellationToken);

        return new PriceCaptureDto(
            capture.Id,
            capture.CaseId,
            capture.CapturedByUserId,
            capture.CaptureType.ToString(),
            capture.EvidenceType.ToString(),
            capture.QuotedAmount,
            capture.CurrencyCode,
            capture.FileName,
            capture.ContentType,
            capture.EvidenceStoragePath,
            capture.EvidenceHash,
            capture.MerchantStatement,
            capture.Notes,
            capture.CapturedAtUtc,
            capture.CreatedUtc,
            discrepancyCase.Classification.ToString(),
            discrepancyCase.DifferenceAmount);
    }
}
