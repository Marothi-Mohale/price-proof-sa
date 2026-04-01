using PriceProof.Domain.Entities;

namespace PriceProof.Application.Cases;

internal static class CaseMappings
{
    public static CaseSummaryDto ToSummaryDto(this DiscrepancyCase entity)
    {
        return new CaseSummaryDto(
            entity.Id,
            entity.CaseNumber,
            entity.Merchant!.ToReferenceDto(),
            entity.Branch?.ToReferenceDto(),
            entity.BasketDescription,
            entity.Status.ToString(),
            entity.Classification.ToString(),
            entity.LatestQuotedAmount,
            entity.LatestPaidAmount,
            entity.DifferenceAmount,
            entity.IncidentAtUtc,
            entity.CreatedUtc,
            entity.UpdatedUtc);
    }

    public static CaseDetailDto ToDetailDto(this DiscrepancyCase entity)
    {
        return new CaseDetailDto(
            entity.Id,
            entity.CaseNumber,
            entity.ReportedByUser!.ToReferenceDto(),
            entity.Merchant!.ToReferenceDto(),
            entity.Branch?.ToReferenceDto(),
            entity.BasketDescription,
            entity.CurrencyCode,
            entity.Status.ToString(),
            entity.Classification.ToString(),
            entity.LatestQuotedAmount,
            entity.LatestPaidAmount,
            entity.DifferenceAmount,
            entity.IncidentAtUtc,
            entity.CustomerReference,
            entity.Notes,
            entity.CreatedUtc,
            entity.UpdatedUtc,
            entity.PriceCaptures
                .OrderByDescending(capture => capture.CapturedAtUtc)
                .ThenByDescending(capture => capture.CreatedUtc)
                .Select(capture => capture.ToDto())
                .ToArray(),
            entity.PaymentRecords
                .OrderByDescending(record => record.PaidAtUtc)
                .ThenByDescending(record => record.CreatedUtc)
                .Select(record => record.ToDto())
                .ToArray(),
            entity.ComplaintPacks
                .OrderByDescending(pack => pack.GeneratedAtUtc)
                .Select(pack => pack.ToDto())
                .ToArray(),
            entity.AuditLogs
                .OrderByDescending(log => log.OccurredAtUtc)
                .ThenByDescending(log => log.CreatedUtc)
                .Select(log => log.ToDto())
                .ToArray());
    }

    public static UserReferenceDto ToReferenceDto(this User entity) => new(entity.Id, entity.DisplayName, entity.Email);

    public static MerchantReferenceDto ToReferenceDto(this Merchant entity) => new(entity.Id, entity.Name, entity.Category, entity.WebsiteUrl);

    public static BranchReferenceDto ToReferenceDto(this Branch entity) => new(
        entity.Id,
        entity.MerchantId,
        entity.Name,
        entity.Code,
        entity.AddressLine1,
        entity.AddressLine2,
        entity.City,
        entity.Province,
        entity.PostalCode);

    public static PriceCaptureSummaryDto ToDto(this PriceCapture entity) => new(
        entity.Id,
        entity.CaptureType.ToString(),
        entity.EvidenceType.ToString(),
        entity.QuotedAmount,
        entity.CurrencyCode,
        entity.FileName,
        entity.ContentType,
        entity.EvidenceStoragePath,
        entity.MerchantStatement,
        entity.Notes,
        entity.CapturedAtUtc,
        entity.CreatedUtc);

    public static PaymentRecordSummaryDto ToDto(this PaymentRecord entity) => new(
        entity.Id,
        entity.PaymentMethod.ToString(),
        entity.Amount,
        entity.CurrencyCode,
        entity.PaymentReference,
        entity.MerchantReference,
        entity.CardLastFour,
        entity.Notes,
        entity.PaidAtUtc,
        entity.CreatedUtc,
        entity.ReceiptRecord?.ToDto());

    public static ReceiptSummaryDto ToDto(this ReceiptRecord entity) => new(
        entity.Id,
        entity.EvidenceType.ToString(),
        entity.FileName,
        entity.ContentType,
        entity.StoragePath,
        entity.CurrencyCode,
        entity.ParsedTotalAmount,
        entity.ReceiptNumber,
        entity.MerchantName,
        entity.RawText,
        entity.UploadedAtUtc,
        entity.CreatedUtc);

    public static ComplaintPackDto ToDto(this ComplaintPack entity) => new(
        entity.Id,
        entity.FileName,
        entity.StoragePath,
        entity.ContentHash,
        entity.Summary,
        $"/complaint-packs/{entity.Id}/download",
        entity.GeneratedAtUtc);

    public static AuditLogDto ToDto(this AuditLog entity) => new(
        entity.Id,
        entity.EntityName,
        entity.Action,
        entity.CorrelationId,
        entity.OccurredAtUtc,
        entity.CreatedUtc);
}
