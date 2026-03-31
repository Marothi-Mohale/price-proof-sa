using PriceProofSA.Domain.Common;
using PriceProofSA.Domain.Enums;

namespace PriceProofSA.Domain.Entities;

public sealed class PriceCapture : BaseEntity
{
    private readonly List<PriceEvidence> _evidence = [];

    private PriceCapture()
    {
    }

    public Guid CaseId { get; private set; }

    public DiscrepancyCase? Case { get; private set; }

    public PriceCaptureMode Mode { get; private set; }

    public decimal? CapturedAmount { get; private set; }

    public string CurrencyCode { get; private set; } = "ZAR";

    public string? QuoteText { get; private set; }

    public string? Notes { get; private set; }

    public bool IsLocked { get; private set; }

    public string? MerchantQrToken { get; private set; }

    public DateTimeOffset CapturedAtUtc { get; private set; }

    public IReadOnlyCollection<PriceEvidence> Evidence => _evidence;

    public static PriceCapture Create(
        Guid caseId,
        PriceCaptureMode mode,
        decimal? capturedAmount,
        string? quoteText,
        string? notes,
        DateTimeOffset capturedAtUtc,
        bool isLocked = true,
        string? merchantQrToken = null)
    {
        return new PriceCapture
        {
            CaseId = caseId,
            Mode = mode,
            CapturedAmount = capturedAmount,
            QuoteText = string.IsNullOrWhiteSpace(quoteText) ? null : quoteText.Trim(),
            Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim(),
            CapturedAtUtc = capturedAtUtc,
            IsLocked = isLocked,
            MerchantQrToken = merchantQrToken
        };
    }

    public void AddEvidence(PriceEvidence evidence)
    {
        _evidence.Add(evidence);
    }
}
