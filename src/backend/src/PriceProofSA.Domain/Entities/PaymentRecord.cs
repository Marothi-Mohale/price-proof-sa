using PriceProofSA.Domain.Common;
using PriceProofSA.Domain.Enums;

namespace PriceProofSA.Domain.Entities;

public sealed class PaymentRecord : BaseEntity
{
    private PaymentRecord()
    {
    }

    public Guid CaseId { get; private set; }

    public DiscrepancyCase? Case { get; private set; }

    public PaymentInputMode Mode { get; private set; }

    public decimal? Amount { get; private set; }

    public string CurrencyCode { get; private set; } = "ZAR";

    public bool IsCardPayment { get; private set; }

    public string? Note { get; private set; }

    public string? RedactedBankNotificationText { get; private set; }

    public DateTimeOffset CapturedAtUtc { get; private set; }

    public ReceiptRecord? ReceiptRecord { get; private set; }

    public static PaymentRecord Create(
        Guid caseId,
        PaymentInputMode mode,
        decimal? amount,
        bool isCardPayment,
        string? note,
        string? redactedBankNotificationText,
        DateTimeOffset capturedAtUtc)
    {
        return new PaymentRecord
        {
            CaseId = caseId,
            Mode = mode,
            Amount = amount,
            IsCardPayment = isCardPayment,
            Note = string.IsNullOrWhiteSpace(note) ? null : note.Trim(),
            RedactedBankNotificationText = string.IsNullOrWhiteSpace(redactedBankNotificationText)
                ? null
                : redactedBankNotificationText.Trim(),
            CapturedAtUtc = capturedAtUtc
        };
    }

    public void ResolveAmount(decimal amount)
    {
        Amount = amount;
    }

    public void AttachReceipt(ReceiptRecord receiptRecord)
    {
        ReceiptRecord = receiptRecord;
    }
}
