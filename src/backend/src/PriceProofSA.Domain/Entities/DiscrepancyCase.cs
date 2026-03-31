using PriceProofSA.Domain.Common;
using PriceProofSA.Domain.Enums;
using PriceProofSA.Domain.ValueObjects;

namespace PriceProofSA.Domain.Entities;

public sealed class DiscrepancyCase : BaseEntity
{
    private readonly List<PriceCapture> _priceCaptures = [];
    private readonly List<PaymentRecord> _paymentRecords = [];
    private readonly List<ComplaintPack> _complaintPacks = [];

    private DiscrepancyCase()
    {
    }

    public Guid UserId { get; private set; }

    public AppUser? User { get; private set; }

    public Guid MerchantId { get; private set; }

    public Merchant? Merchant { get; private set; }

    public Guid? BranchId { get; private set; }

    public Branch? Branch { get; private set; }

    public string BasketDescription { get; private set; } = string.Empty;

    public CaseStatus Status { get; private set; }

    public decimal? QuotedAmount { get; private set; }

    public decimal? ChargedAmount { get; private set; }

    public decimal? DifferenceAmount { get; private set; }

    public DiscrepancyClassification Classification { get; private set; }

    public bool LikelyUnlawfulCardSurcharge { get; private set; }

    public string? ComplaintSummary { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public DateTimeOffset UpdatedAtUtc { get; private set; }

    public DateTimeOffset? ClosedAtUtc { get; private set; }

    public IReadOnlyCollection<PriceCapture> PriceCaptures => _priceCaptures;

    public IReadOnlyCollection<PaymentRecord> PaymentRecords => _paymentRecords;

    public IReadOnlyCollection<ComplaintPack> ComplaintPacks => _complaintPacks;

    public static DiscrepancyCase Create(
        Guid userId,
        Guid merchantId,
        Guid? branchId,
        string basketDescription,
        DateTimeOffset now)
    {
        return new DiscrepancyCase
        {
            UserId = userId,
            MerchantId = merchantId,
            BranchId = branchId,
            BasketDescription = basketDescription.Trim(),
            Status = CaseStatus.Draft,
            Classification = DiscrepancyClassification.NoMismatch,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };
    }

    public void AddPriceCapture(PriceCapture capture, DateTimeOffset now)
    {
        _priceCaptures.Add(capture);
        if (capture.CapturedAmount.HasValue)
        {
            QuotedAmount = capture.CapturedAmount.Value;
        }

        Status = CaseStatus.PendingPayment;
        UpdatedAtUtc = now;
    }

    public void AddPaymentRecord(PaymentRecord paymentRecord, DateTimeOffset now)
    {
        _paymentRecords.Add(paymentRecord);
        if (paymentRecord.Amount.HasValue)
        {
            ChargedAmount = paymentRecord.Amount.Value;
            Status = CaseStatus.PendingAnalysis;
        }
        else
        {
            Status = CaseStatus.PendingAnalysis;
        }

        UpdatedAtUtc = now;
    }

    public void ApplyAnalysis(DiscrepancyAnalysis analysis, DateTimeOffset now)
    {
        QuotedAmount = analysis.QuotedAmount;
        ChargedAmount = analysis.ChargedAmount;
        DifferenceAmount = analysis.DifferenceAmount;
        Classification = analysis.Classification;
        LikelyUnlawfulCardSurcharge = analysis.LikelyUnlawfulCardSurcharge;
        ComplaintSummary = analysis.Explanation;
        Status = CaseStatus.ReadyForComplaint;
        UpdatedAtUtc = now;
    }

    public void AttachComplaintPack(ComplaintPack complaintPack, DateTimeOffset now)
    {
        _complaintPacks.Add(complaintPack);
        UpdatedAtUtc = now;
    }

    public void Close(DateTimeOffset now)
    {
        Status = CaseStatus.Closed;
        ClosedAtUtc = now;
        UpdatedAtUtc = now;
    }
}
