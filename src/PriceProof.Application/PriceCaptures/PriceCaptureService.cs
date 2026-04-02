using Microsoft.EntityFrameworkCore;
using PriceProof.Application.Abstractions.Diagnostics;
using PriceProof.Application.Abstractions.Persistence;
using PriceProof.Application.Abstractions.Security;
using PriceProof.Application.Abstractions.Services;
using PriceProof.Application.Common;
using PriceProof.Application.Common.Exceptions;
using PriceProof.Domain.Entities;

namespace PriceProof.Application.PriceCaptures;

internal sealed class PriceCaptureService : IPriceCaptureService
{
    private readonly IAuditLogWriter _auditLogWriter;
    private readonly ICurrentUserContext _currentUserContext;
    private readonly IApplicationDbContext _dbContext;

    public PriceCaptureService(IApplicationDbContext dbContext, ICurrentUserContext currentUserContext, IAuditLogWriter auditLogWriter)
    {
        _dbContext = dbContext;
        _currentUserContext = currentUserContext;
        _auditLogWriter = auditLogWriter;
    }

    public async Task<PriceCaptureDto> CreateAsync(CreatePriceCaptureRequest request, CancellationToken cancellationToken)
    {
        request = request with
        {
            CurrencyCode = InputSanitizer.SanitizeCurrencyCode(request.CurrencyCode),
            FileName = InputSanitizer.SanitizeRequiredSingleLine(request.FileName, 260),
            EvidenceStoragePath = InputSanitizer.SanitizeRequiredSingleLine(request.EvidenceStoragePath, 500),
            ContentType = InputSanitizer.SanitizeSingleLine(request.ContentType, 120),
            EvidenceHash = InputSanitizer.SanitizeHash(request.EvidenceHash, 128),
            MerchantStatement = InputSanitizer.SanitizeMultiline(request.MerchantStatement, 2000),
            Notes = InputSanitizer.SanitizeMultiline(request.Notes, 2000)
        };

        var discrepancyCase = await _dbContext.DiscrepancyCases
            .Include(entity => entity.PaymentRecords)
                .ThenInclude(record => record.ReceiptRecord)
            .SingleOrDefaultAsync(entity => entity.Id == request.CaseId, cancellationToken);

        if (discrepancyCase is null)
        {
            throw new NotFoundException($"Case '{request.CaseId}' was not found.");
        }

        CurrentUserGuards.EnsureCanAccessCase(_currentUserContext, discrepancyCase.ReportedByUserId);
        var currentUserId = CurrentUserGuards.RequireAuthenticatedUserId(_currentUserContext);
        request = request with { CapturedByUserId = currentUserId };

        var capture = PriceCapture.Create(
            request.CaseId,
            currentUserId,
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
        _auditLogWriter.Write(
            nameof(PriceCapture),
            "PriceCaptureCreated",
            request,
            now,
            currentUserId,
            request.CaseId);

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
