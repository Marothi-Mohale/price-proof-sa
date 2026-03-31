using PriceProof.Domain.Common;
using PriceProof.Domain.Enums;

namespace PriceProof.Domain.Entities;

public sealed class PaymentRecord : AuditableEntity
{
    private PaymentRecord()
    {
    }

    public Guid CaseId { get; private set; }

    public DiscrepancyCase? Case { get; private set; }

    public Guid RecordedByUserId { get; private set; }

    public User? RecordedByUser { get; private set; }

    public PaymentMethod PaymentMethod { get; private set; }

    public decimal Amount { get; private set; }

    public string CurrencyCode { get; private set; } = "ZAR";

    public string? PaymentReference { get; private set; }

    public string? MerchantReference { get; private set; }

    public string? CardLastFour { get; private set; }

    public string? Notes { get; private set; }

    public DateTimeOffset PaidAtUtc { get; private set; }

    public ReceiptRecord? ReceiptRecord { get; private set; }

    public static PaymentRecord Create(
        Guid caseId,
        Guid recordedByUserId,
        PaymentMethod paymentMethod,
        decimal amount,
        string currencyCode,
        DateTimeOffset paidAtUtc,
        string? paymentReference = null,
        string? merchantReference = null,
        string? cardLastFour = null,
        string? notes = null)
    {
        return new PaymentRecord
        {
            CaseId = caseId,
            RecordedByUserId = recordedByUserId,
            PaymentMethod = paymentMethod,
            Amount = decimal.Round(amount, 2, MidpointRounding.AwayFromZero),
            CurrencyCode = currencyCode.Trim().ToUpperInvariant(),
            PaidAtUtc = paidAtUtc,
            PaymentReference = Normalize(paymentReference),
            MerchantReference = Normalize(merchantReference),
            CardLastFour = Normalize(cardLastFour),
            Notes = Normalize(notes)
        };
    }

    public void AttachReceipt(ReceiptRecord receiptRecord, DateTimeOffset now)
    {
        ReceiptRecord = receiptRecord;
        UpdatedUtc = now;
    }

    private static string? Normalize(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
