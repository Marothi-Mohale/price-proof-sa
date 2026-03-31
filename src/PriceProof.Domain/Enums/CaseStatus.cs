namespace PriceProof.Domain.Enums;

public enum CaseStatus
{
    Open = 1,
    AwaitingPayment = 2,
    AwaitingReceipt = 3,
    ReadyForReview = 4,
    Closed = 5
}
