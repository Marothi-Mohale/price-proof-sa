using PriceProof.Domain.Common;
using PriceProof.Domain.Enums;

namespace PriceProof.Domain.Entities;

public sealed class PriceCapture : AuditableEntity
{
    private PriceCapture()
    {
    }

    public Guid CaseId { get; private set; }

    public DiscrepancyCase? Case { get; private set; }

    public Guid CapturedByUserId { get; private set; }

    public User? CapturedByUser { get; private set; }

    public CaptureType CaptureType { get; private set; }

    public EvidenceType EvidenceType { get; private set; }

    public decimal? QuotedAmount { get; private set; }

    public string CurrencyCode { get; private set; } = "ZAR";

    public string FileName { get; private set; } = string.Empty;

    public string? ContentType { get; private set; }

    public string EvidenceStoragePath { get; private set; } = string.Empty;

    public string? EvidenceHash { get; private set; }

    public string? MerchantStatement { get; private set; }

    public string? Notes { get; private set; }

    public DateTimeOffset CapturedAtUtc { get; private set; }

    public static PriceCapture Create(
        Guid caseId,
        Guid capturedByUserId,
        CaptureType captureType,
        EvidenceType evidenceType,
        decimal? quotedAmount,
        string currencyCode,
        string fileName,
        string evidenceStoragePath,
        DateTimeOffset capturedAtUtc,
        string? contentType = null,
        string? evidenceHash = null,
        string? merchantStatement = null,
        string? notes = null)
    {
        return new PriceCapture
        {
            CaseId = caseId,
            CapturedByUserId = capturedByUserId,
            CaptureType = captureType,
            EvidenceType = evidenceType,
            QuotedAmount = quotedAmount,
            CurrencyCode = currencyCode.Trim().ToUpperInvariant(),
            FileName = fileName.Trim(),
            ContentType = Normalize(contentType),
            EvidenceStoragePath = evidenceStoragePath.Trim(),
            EvidenceHash = Normalize(evidenceHash),
            MerchantStatement = Normalize(merchantStatement),
            Notes = Normalize(notes),
            CapturedAtUtc = capturedAtUtc
        };
    }

    private static string? Normalize(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
