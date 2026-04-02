using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using PriceProof.Application.Abstractions.ComplaintPacks;
using PriceProof.Application.Abstractions.Diagnostics;
using PriceProof.Application.Abstractions.Persistence;
using PriceProof.Application.Abstractions.Services;
using PriceProof.Application.Common.Exceptions;
using PriceProof.Domain.Entities;
using PriceProof.Domain.Enums;
using PriceProof.Domain.Services;

namespace PriceProof.Application.ComplaintPacks;

internal sealed class ComplaintPackService : IComplaintPackService
{
    private readonly IAuditLogWriter _auditLogWriter;
    private readonly IApplicationDbContext _dbContext;
    private readonly IComplaintPackGenerator _complaintPackGenerator;
    private readonly IComplaintPackDocumentStore _documentStore;
    private readonly IComplaintNarrativeComposer _complaintNarrativeComposer;

    public ComplaintPackService(
        IApplicationDbContext dbContext,
        IComplaintPackGenerator complaintPackGenerator,
        IComplaintPackDocumentStore documentStore,
        IComplaintNarrativeComposer complaintNarrativeComposer,
        IAuditLogWriter auditLogWriter)
    {
        _dbContext = dbContext;
        _complaintPackGenerator = complaintPackGenerator;
        _documentStore = documentStore;
        _complaintNarrativeComposer = complaintNarrativeComposer;
        _auditLogWriter = auditLogWriter;
    }

    public async Task<GeneratedComplaintPackDto> GenerateAsync(Guid caseId, CancellationToken cancellationToken)
    {
        var discrepancyCase = await _dbContext.DiscrepancyCases
            .Include(entity => entity.ReportedByUser)
            .Include(entity => entity.Merchant)
            .Include(entity => entity.Branch)
            .Include(entity => entity.PriceCaptures)
            .Include(entity => entity.PaymentRecords)
                .ThenInclude(record => record.ReceiptRecord)
            .SingleOrDefaultAsync(entity => entity.Id == caseId, cancellationToken);

        if (discrepancyCase is null)
        {
            throw new NotFoundException($"Case '{caseId}' was not found.");
        }

        if (!discrepancyCase.LatestQuotedAmount.HasValue || !discrepancyCase.LatestPaidAmount.HasValue)
        {
            throw new ConflictException("A case needs both a quoted amount and a charged amount before a complaint pack can be generated.");
        }

        var now = DateTimeOffset.UtcNow;
        var quotedAmount = discrepancyCase.LatestQuotedAmount.Value;
        var chargedAmount = discrepancyCase.LatestPaidAmount.Value;
        var differenceAmount = discrepancyCase.DifferenceAmount
            ?? decimal.Round(chargedAmount - quotedAmount, 2, MidpointRounding.AwayFromZero);
        decimal? percentageDifference = quotedAmount == 0
            ? null
            : decimal.Round((differenceAmount / quotedAmount) * 100m, 2, MidpointRounding.AwayFromZero);

        var explanation = BuildExplanation(discrepancyCase, differenceAmount, percentageDifference);
        var classification = discrepancyCase.AnalysisClassification?.ToString() ?? discrepancyCase.Classification.ToString();
        var classificationLabel = HumanizeLabel(classification);

        var evidenceInventory = BuildEvidenceInventory(discrepancyCase);
        var narrative = _complaintNarrativeComposer.Compose(new ComplaintNarrativeInput(
            discrepancyCase.CaseNumber,
            discrepancyCase.Merchant!.Name,
            discrepancyCase.Branch?.Name,
            discrepancyCase.IncidentAtUtc,
            discrepancyCase.CurrencyCode,
            quotedAmount,
            chargedAmount,
            differenceAmount,
            percentageDifference,
            classificationLabel,
            explanation,
            discrepancyCase.PriceCaptures.Any(capture => capture.QuotedAmount.HasValue && !string.IsNullOrWhiteSpace(capture.EvidenceStoragePath)),
            discrepancyCase.PaymentRecords.Any(record => record.ReceiptRecord is not null),
            discrepancyCase.PaymentRecords.Any(record => record.ReceiptRecord is not null &&
                                                         (record.ReceiptRecord!.ParsedTotalAmount.HasValue ||
                                                          !string.IsNullOrWhiteSpace(record.ReceiptRecord.RawText))),
            evidenceInventory.Count));

        var timeline = BuildTimeline(discrepancyCase, classificationLabel, now);
        var jsonSummary = BuildJsonSummaryDto(
            discrepancyCase,
            quotedAmount,
            chargedAmount,
            differenceAmount,
            percentageDifference,
            classification,
            classificationLabel,
            narrative,
            explanation,
            timeline,
            evidenceInventory,
            now);

        var document = await _complaintPackGenerator.GenerateAsync(
            new ComplaintPackBuildRequest(
                discrepancyCase.CaseNumber,
                discrepancyCase.Merchant!.Name,
                discrepancyCase.Merchant.Category,
                discrepancyCase.Merchant.WebsiteUrl,
                discrepancyCase.Branch?.Name,
                discrepancyCase.Branch?.Code,
                discrepancyCase.Branch?.AddressLine1,
                discrepancyCase.Branch?.AddressLine2,
                discrepancyCase.Branch?.City,
                discrepancyCase.Branch?.Province,
                discrepancyCase.Branch?.PostalCode,
                discrepancyCase.ReportedByUser!.DisplayName,
                discrepancyCase.ReportedByUser.Email,
                discrepancyCase.BasketDescription,
                discrepancyCase.IncidentAtUtc,
                discrepancyCase.CurrencyCode,
                quotedAmount,
                chargedAmount,
                differenceAmount,
                percentageDifference,
                classification,
                classificationLabel,
                discrepancyCase.AnalysisConfidence,
                explanation,
                narrative.EvidenceStrength.ToString(),
                narrative.EvidenceStrengthExplanation,
                timeline.Select(item => new ComplaintPackTimelineEntry(item.OccurredAtUtc, item.Title, item.Description)).ToArray(),
                evidenceInventory.Select(item => new ComplaintPackEvidenceEntry(
                    item.Category,
                    item.Label,
                    item.FileName,
                    item.ContentType,
                    item.StoragePath,
                    item.RecordedAtUtc,
                    item.ReferenceLink,
                    item.CurrencyCode,
                    item.Amount,
                    item.Notes)).ToArray(),
                narrative.ComplaintSummary,
                narrative.DeclarationText,
                now),
            cancellationToken);

        var storedDocument = await _documentStore.SaveAsync(discrepancyCase.Id, document, cancellationToken);

        var complaintPack = ComplaintPack.Create(
            discrepancyCase.Id,
            discrepancyCase.ReportedByUserId,
            storedDocument.FileName,
            storedDocument.StoragePath,
            storedDocument.ContentHash,
            narrative.ComplaintSummary,
            now);

        discrepancyCase.AddComplaintPack(complaintPack, now);
        _dbContext.ComplaintPacks.Add(complaintPack);
        _auditLogWriter.Write(
            nameof(ComplaintPack),
            "ComplaintPackGenerated",
            new
            {
                ComplaintPackId = complaintPack.Id,
                discrepancyCase.CaseNumber,
                storedDocument.FileName,
                storedDocument.StoragePath,
                storedDocument.ContentHash,
                storedDocument.SizeBytes,
                Classification = classification,
                EvidenceStrength = narrative.EvidenceStrength.ToString(),
                Summary = narrative.ComplaintSummary
            },
            now,
            discrepancyCase.ReportedByUserId,
            discrepancyCase.Id);

        await _dbContext.SaveChangesAsync(cancellationToken);

        return new GeneratedComplaintPackDto(
            complaintPack.Id,
            discrepancyCase.Id,
            discrepancyCase.CaseNumber,
            storedDocument.FileName,
            document.ContentType,
            BuildDownloadUrl(complaintPack.Id),
            storedDocument.ContentHash,
            storedDocument.SizeBytes,
            narrative.ComplaintSummary,
            jsonSummary,
            complaintPack.GeneratedAtUtc);
    }

    public async Task<ComplaintPackDownloadDto> DownloadAsync(Guid complaintPackId, CancellationToken cancellationToken)
    {
        var complaintPack = await _dbContext.ComplaintPacks
            .AsNoTracking()
            .SingleOrDefaultAsync(entity => entity.Id == complaintPackId, cancellationToken);

        if (complaintPack is null)
        {
            throw new NotFoundException($"Complaint pack '{complaintPackId}' was not found.");
        }

        var file = await _documentStore.DownloadAsync(complaintPack.FileName, complaintPack.StoragePath, cancellationToken);
        return new ComplaintPackDownloadDto(file.FileName, file.ContentType, file.Content);
    }

    private static ComplaintPackJsonSummaryDto BuildJsonSummaryDto(
        DiscrepancyCase discrepancyCase,
        decimal quotedAmount,
        decimal chargedAmount,
        decimal differenceAmount,
        decimal? percentageDifference,
        string classification,
        string classificationLabel,
        ComplaintNarrativeResult narrative,
        string explanation,
        IReadOnlyCollection<ComplaintPackTimelineItemDto> timeline,
        IReadOnlyCollection<ComplaintPackEvidenceItemDto> evidenceInventory,
        DateTimeOffset auditTimestampUtc)
    {
        return new ComplaintPackJsonSummaryDto(
            discrepancyCase.Id,
            discrepancyCase.CaseNumber,
            new ComplaintPackLocationDto(
                discrepancyCase.Merchant!.Name,
                discrepancyCase.Merchant.Category,
                discrepancyCase.Merchant.WebsiteUrl,
                discrepancyCase.Branch?.Name,
                discrepancyCase.Branch?.Code,
                discrepancyCase.Branch?.AddressLine1,
                discrepancyCase.Branch?.AddressLine2,
                discrepancyCase.Branch?.City,
                discrepancyCase.Branch?.Province,
                discrepancyCase.Branch?.PostalCode),
            new ComplaintPackAmountsDto(
                discrepancyCase.CurrencyCode,
                quotedAmount,
                chargedAmount,
                differenceAmount,
                percentageDifference),
            new ComplaintPackAnalysisDto(
                classification,
                classificationLabel,
                discrepancyCase.AnalysisConfidence,
                explanation),
            new ComplaintPackEvidenceAssessmentDto(
                narrative.EvidenceStrength.ToString(),
                narrative.EvidenceStrengthExplanation),
            timeline,
            evidenceInventory,
            narrative.ComplaintSummary,
            narrative.DeclarationText,
            auditTimestampUtc);
    }

    private static IReadOnlyCollection<ComplaintPackTimelineItemDto> BuildTimeline(
        DiscrepancyCase discrepancyCase,
        string classificationLabel,
        DateTimeOffset generatedAtUtc)
    {
        var items = new List<ComplaintPackTimelineItemDto>
        {
            new(
                discrepancyCase.IncidentAtUtc,
                "Incident recorded",
                $"Case {discrepancyCase.CaseNumber} references a pricing discrepancy for {discrepancyCase.BasketDescription}.")
        };

        items.AddRange(discrepancyCase.PriceCaptures
            .OrderBy(capture => capture.CapturedAtUtc)
            .ThenBy(capture => capture.CreatedUtc)
            .Select(capture => new ComplaintPackTimelineItemDto(
                capture.CapturedAtUtc,
                "Quoted price captured",
                capture.QuotedAmount.HasValue
                    ? $"{HumanizeLabel(capture.CaptureType.ToString())} recorded {FormatMoney(capture.CurrencyCode, capture.QuotedAmount.Value)} from {capture.FileName}."
                    : $"{HumanizeLabel(capture.CaptureType.ToString())} evidence was recorded from {capture.FileName}.")));

        items.AddRange(discrepancyCase.PaymentRecords
            .OrderBy(record => record.PaidAtUtc)
            .ThenBy(record => record.CreatedUtc)
            .Select(record => new ComplaintPackTimelineItemDto(
                record.PaidAtUtc,
                "Payment recorded",
                $"{HumanizeLabel(record.PaymentMethod.ToString())} payment recorded at {FormatMoney(record.CurrencyCode, record.Amount)}.")));

        items.AddRange(discrepancyCase.PaymentRecords
            .Where(record => record.ReceiptRecord is not null)
            .Select(record => record.ReceiptRecord!)
            .OrderBy(receipt => receipt.UploadedAtUtc)
            .ThenBy(receipt => receipt.CreatedUtc)
            .Select(receipt => new ComplaintPackTimelineItemDto(
                receipt.UploadedAtUtc,
                "Receipt uploaded",
                $"Receipt evidence {receipt.FileName} was uploaded.")));

        items.AddRange(discrepancyCase.PaymentRecords
            .Where(record => record.ReceiptRecord?.OcrProcessedUtc.HasValue == true)
            .Select(record => record.ReceiptRecord!)
            .OrderBy(receipt => receipt.OcrProcessedUtc)
            .Select(receipt => new ComplaintPackTimelineItemDto(
                receipt.OcrProcessedUtc!.Value,
                "Receipt OCR completed",
                receipt.ParsedTotalAmount.HasValue
                    ? $"OCR extracted a charged amount of {FormatMoney(receipt.CurrencyCode, receipt.ParsedTotalAmount.Value)}."
                    : "OCR processed the receipt evidence without confirming a final total.")));

        if (discrepancyCase.AnalysisUpdatedUtc.HasValue)
        {
            items.Add(new ComplaintPackTimelineItemDto(
                discrepancyCase.AnalysisUpdatedUtc.Value,
                "Case analyzed",
                $"PriceProof SA classified the discrepancy as {classificationLabel}."));
        }

        items.Add(new ComplaintPackTimelineItemDto(
            generatedAtUtc,
            "Complaint pack generated",
            "This evidence pack was assembled for external review."));

        return items
            .OrderBy(item => item.OccurredAtUtc)
            .ToArray();
    }

    private static IReadOnlyCollection<ComplaintPackEvidenceItemDto> BuildEvidenceInventory(DiscrepancyCase discrepancyCase)
    {
        var evidence = new List<ComplaintPackEvidenceItemDto>();

        evidence.AddRange(discrepancyCase.PriceCaptures
            .OrderBy(capture => capture.CapturedAtUtc)
            .ThenBy(capture => capture.CreatedUtc)
            .Select(capture => new ComplaintPackEvidenceItemDto(
                "QuotedPriceEvidence",
                HumanizeLabel(capture.CaptureType.ToString()),
                capture.FileName,
                capture.ContentType,
                capture.EvidenceStoragePath,
                BuildReferenceLink(capture.EvidenceStoragePath),
                capture.CapturedAtUtc,
                capture.CurrencyCode,
                capture.QuotedAmount,
                capture.MerchantStatement ?? capture.Notes)));

        evidence.AddRange(discrepancyCase.PaymentRecords
            .Where(record => record.ReceiptRecord is not null)
            .Select(record => record.ReceiptRecord!)
            .OrderBy(receipt => receipt.UploadedAtUtc)
            .ThenBy(receipt => receipt.CreatedUtc)
            .Select(receipt => new ComplaintPackEvidenceItemDto(
                "ReceiptEvidence",
                "Receipt upload",
                receipt.FileName,
                receipt.ContentType,
                receipt.StoragePath,
                BuildReferenceLink(receipt.StoragePath),
                receipt.UploadedAtUtc,
                receipt.CurrencyCode,
                receipt.ParsedTotalAmount,
                receipt.MerchantName ?? receipt.ReceiptNumber)));

        return evidence.ToArray();
    }

    private static string BuildExplanation(DiscrepancyCase discrepancyCase, decimal differenceAmount, decimal? percentageDifference)
    {
        if (!string.IsNullOrWhiteSpace(discrepancyCase.AnalysisExplanation))
        {
            return discrepancyCase.AnalysisExplanation!;
        }

        if (differenceAmount == 0m)
        {
            return "The recorded charged amount matches the recorded quoted amount.";
        }

        if (differenceAmount < 0m)
        {
            return "The recorded charged amount is lower than the recorded quoted amount.";
        }

        if (percentageDifference.HasValue)
        {
            return $"{FormatMoney(discrepancyCase.CurrencyCode, discrepancyCase.LatestPaidAmount!.Value)} was recorded against a quoted amount of {FormatMoney(discrepancyCase.CurrencyCode, discrepancyCase.LatestQuotedAmount!.Value)}, a positive difference of {percentageDifference.Value:0.00}%.";
        }

        return "The recorded charged amount is higher than the recorded quoted amount, and the currently available evidence does not conclusively explain the reason for the difference.";
    }

    private static string? BuildReferenceLink(string storagePath)
    {
        return Uri.TryCreate(storagePath, UriKind.Absolute, out var uri) &&
               (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps)
            ? storagePath
            : null;
    }

    private static string BuildDownloadUrl(Guid complaintPackId) => $"/complaint-packs/{complaintPackId}/download";

    private static string HumanizeLabel(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return value;
        }

        return string.Concat(value.Select((character, index) =>
            index > 0 && char.IsUpper(character) && !char.IsUpper(value[index - 1])
                ? $" {character}"
                : character.ToString()));
    }

    private static string FormatMoney(string currencyCode, decimal amount)
    {
        return $"{currencyCode.ToUpperInvariant()} {amount:0.00}";
    }
}
