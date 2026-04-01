using PriceProof.Domain.Common;
using PriceProof.Domain.Enums;

namespace PriceProof.Domain.Entities;

public sealed class DiscrepancyCase : SoftDeletableEntity
{
    private readonly List<PriceCapture> _priceCaptures = [];
    private readonly List<PaymentRecord> _paymentRecords = [];
    private readonly List<ComplaintPack> _complaintPacks = [];
    private readonly List<AuditLog> _auditLogs = [];

    private DiscrepancyCase()
    {
    }

    public Guid ReportedByUserId { get; private set; }

    public User? ReportedByUser { get; private set; }

    public Guid MerchantId { get; private set; }

    public Merchant? Merchant { get; private set; }

    public Guid? BranchId { get; private set; }

    public Branch? Branch { get; private set; }

    public string CaseNumber { get; private set; } = string.Empty;

    public string BasketDescription { get; private set; } = string.Empty;

    public DateTimeOffset IncidentAtUtc { get; private set; }

    public string CurrencyCode { get; private set; } = "ZAR";

    public decimal? LatestQuotedAmount { get; private set; }

    public decimal? LatestPaidAmount { get; private set; }

    public decimal? DifferenceAmount { get; private set; }

    public CaseClassification Classification { get; private set; }

    public CaseStatus Status { get; private set; }

    public string? CustomerReference { get; private set; }

    public string? Notes { get; private set; }

    public DateTimeOffset? ClosedUtc { get; private set; }

    public IReadOnlyCollection<PriceCapture> PriceCaptures => _priceCaptures;

    public IReadOnlyCollection<PaymentRecord> PaymentRecords => _paymentRecords;

    public IReadOnlyCollection<ComplaintPack> ComplaintPacks => _complaintPacks;

    public IReadOnlyCollection<AuditLog> AuditLogs => _auditLogs;

    public static DiscrepancyCase Create(
        Guid reportedByUserId,
        Guid merchantId,
        Guid? branchId,
        string basketDescription,
        DateTimeOffset incidentAtUtc,
        string currencyCode,
        string? customerReference,
        string? notes)
    {
        return new DiscrepancyCase
        {
            ReportedByUserId = reportedByUserId,
            MerchantId = merchantId,
            BranchId = branchId,
            CaseNumber = BuildCaseNumber(),
            BasketDescription = basketDescription.Trim(),
            IncidentAtUtc = incidentAtUtc,
            CurrencyCode = currencyCode.Trim().ToUpperInvariant(),
            CustomerReference = Normalize(customerReference),
            Notes = Normalize(notes),
            Classification = CaseClassification.PendingEvidence,
            Status = CaseStatus.Open
        };
    }

    public void AddPriceCapture(PriceCapture capture, DateTimeOffset now)
    {
        _priceCaptures.Add(capture);

        if (capture.QuotedAmount.HasValue)
        {
            LatestQuotedAmount = capture.QuotedAmount.Value;
        }

        UpdatedUtc = now;
        Recalculate(now);
    }

    public void AddPaymentRecord(PaymentRecord paymentRecord, DateTimeOffset now)
    {
        _paymentRecords.Add(paymentRecord);
        LatestPaidAmount = paymentRecord.Amount;
        UpdatedUtc = now;
        Recalculate(now);
    }

    public void AddComplaintPack(ComplaintPack complaintPack, DateTimeOffset now)
    {
        _complaintPacks.Add(complaintPack);
        UpdatedUtc = now;
    }

    public void Close(DateTimeOffset now)
    {
        Status = CaseStatus.Closed;
        ClosedUtc = now;
        UpdatedUtc = now;
    }

    public void MarkReceiptReceived(DateTimeOffset now)
    {
        if (!LatestQuotedAmount.HasValue || !LatestPaidAmount.HasValue)
        {
            return;
        }

        Status = CaseStatus.ReadyForReview;
        UpdatedUtc = now;
    }

    private void Recalculate(DateTimeOffset now)
    {
        if (!LatestQuotedAmount.HasValue)
        {
            DifferenceAmount = null;
            Classification = CaseClassification.PendingEvidence;
            Status = CaseStatus.Open;
            return;
        }

        if (!LatestPaidAmount.HasValue)
        {
            DifferenceAmount = null;
            Classification = CaseClassification.PendingEvidence;
            Status = CaseStatus.AwaitingPayment;
            return;
        }

        var difference = decimal.Round(LatestPaidAmount.Value - LatestQuotedAmount.Value, 2, MidpointRounding.AwayFromZero);
        DifferenceAmount = difference;
        Status = _paymentRecords.Any(record => record.ReceiptRecord is not null)
            ? CaseStatus.ReadyForReview
            : CaseStatus.AwaitingReceipt;

        if (difference == 0)
        {
            Classification = CaseClassification.Match;
            return;
        }

        if (difference < 0)
        {
            Classification = CaseClassification.Undercharge;
            return;
        }

        var latestPayment = _paymentRecords
            .OrderByDescending(record => record.PaidAtUtc)
            .ThenByDescending(record => record.CreatedUtc)
            .FirstOrDefault();

        Classification = latestPayment is not null &&
                         latestPayment.PaymentMethod is PaymentMethod.CreditCard or PaymentMethod.DebitCard
            ? CaseClassification.PotentialCardSurcharge
            : CaseClassification.Overcharge;
    }

    private static string BuildCaseNumber()
    {
        return $"PP-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid():N}"[..20];
    }

    private static string? Normalize(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
