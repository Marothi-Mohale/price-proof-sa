using PriceProof.Domain.Common;
using PriceProof.Domain.Enums;

namespace PriceProof.Domain.Entities;

public sealed class ReceiptRecord : AuditableEntity
{
    private ReceiptRecord()
    {
    }

    public Guid CaseId { get; private set; }

    public DiscrepancyCase? Case { get; private set; }

    public Guid PaymentRecordId { get; private set; }

    public PaymentRecord? PaymentRecord { get; private set; }

    public Guid UploadedByUserId { get; private set; }

    public User? UploadedByUser { get; private set; }

    public EvidenceType EvidenceType { get; private set; }

    public string FileName { get; private set; } = string.Empty;

    public string ContentType { get; private set; } = string.Empty;

    public string StoragePath { get; private set; } = string.Empty;

    public string? FileHash { get; private set; }

    public string CurrencyCode { get; private set; } = "ZAR";

    public decimal? ParsedTotalAmount { get; private set; }

    public string? ReceiptNumber { get; private set; }

    public string? MerchantName { get; private set; }

    public string? RawText { get; private set; }

    public DateTimeOffset UploadedAtUtc { get; private set; }

    public static ReceiptRecord Create(
        Guid caseId,
        Guid paymentRecordId,
        Guid uploadedByUserId,
        EvidenceType evidenceType,
        string fileName,
        string contentType,
        string storagePath,
        DateTimeOffset uploadedAtUtc,
        string currencyCode,
        decimal? parsedTotalAmount = null,
        string? receiptNumber = null,
        string? merchantName = null,
        string? rawText = null,
        string? fileHash = null)
    {
        return new ReceiptRecord
        {
            CaseId = caseId,
            PaymentRecordId = paymentRecordId,
            UploadedByUserId = uploadedByUserId,
            EvidenceType = evidenceType,
            FileName = fileName.Trim(),
            ContentType = contentType.Trim(),
            StoragePath = storagePath.Trim(),
            UploadedAtUtc = uploadedAtUtc,
            CurrencyCode = currencyCode.Trim().ToUpperInvariant(),
            ParsedTotalAmount = parsedTotalAmount.HasValue
                ? decimal.Round(parsedTotalAmount.Value, 2, MidpointRounding.AwayFromZero)
                : null,
            ReceiptNumber = Normalize(receiptNumber),
            MerchantName = Normalize(merchantName),
            RawText = Normalize(rawText),
            FileHash = Normalize(fileHash)
        };
    }

    private static string? Normalize(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
