using System.Globalization;
using PriceProof.Domain.Enums;

namespace PriceProof.Domain.Services;

public sealed record ComplaintNarrativeInput(
    string CaseNumber,
    string MerchantName,
    string? BranchName,
    DateTimeOffset IncidentAtUtc,
    string CurrencyCode,
    decimal QuotedAmount,
    decimal ChargedAmount,
    decimal DifferenceAmount,
    decimal? PercentageDifference,
    string ClassificationLabel,
    string Explanation,
    bool HasQuotedEvidence,
    bool HasReceiptEvidence,
    bool HasReceiptOcr,
    int EvidenceItemCount);

public sealed record ComplaintNarrativeResult(
    ComplaintEvidenceStrength EvidenceStrength,
    string EvidenceStrengthExplanation,
    string ComplaintSummary,
    string DeclarationText);

public interface IComplaintNarrativeComposer
{
    ComplaintNarrativeResult Compose(ComplaintNarrativeInput input);
}

public sealed class ComplaintNarrativeComposer : IComplaintNarrativeComposer
{
    public ComplaintNarrativeResult Compose(ComplaintNarrativeInput input)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(input.CaseNumber);
        ArgumentException.ThrowIfNullOrWhiteSpace(input.MerchantName);
        ArgumentException.ThrowIfNullOrWhiteSpace(input.CurrencyCode);
        ArgumentException.ThrowIfNullOrWhiteSpace(input.ClassificationLabel);
        ArgumentException.ThrowIfNullOrWhiteSpace(input.Explanation);

        var strength = DetermineEvidenceStrength(input);
        var strengthExplanation = BuildEvidenceStrengthExplanation(strength);
        var complaintSummary = BuildComplaintSummary(input, strengthExplanation);
        const string declarationText = "I confirm that this complaint pack reflects the records and supporting material I submitted to PriceProof SA, to the best of my knowledge.";

        return new ComplaintNarrativeResult(
            strength,
            strengthExplanation,
            complaintSummary,
            declarationText);
    }

    private static ComplaintEvidenceStrength DetermineEvidenceStrength(ComplaintNarrativeInput input)
    {
        if (input.HasQuotedEvidence && input.HasReceiptEvidence && input.HasReceiptOcr)
        {
            return ComplaintEvidenceStrength.Strong;
        }

        if (input.HasQuotedEvidence && input.HasReceiptEvidence && input.EvidenceItemCount >= 2)
        {
            return ComplaintEvidenceStrength.Moderate;
        }

        return ComplaintEvidenceStrength.Weak;
    }

    private static string BuildEvidenceStrengthExplanation(ComplaintEvidenceStrength strength)
    {
        return strength switch
        {
            ComplaintEvidenceStrength.Strong => "This pack includes both a recorded quoted price and a receipt-backed charged amount, providing a reasonably complete factual record.",
            ComplaintEvidenceStrength.Moderate => "This pack includes recorded price and payment evidence, but some supporting material is incomplete or lacks machine-read confirmation.",
            _ => "This pack contains limited supporting evidence. The recorded discrepancy may require additional corroboration before an external reviewer can reach a firm conclusion."
        };
    }

    private static string BuildComplaintSummary(ComplaintNarrativeInput input, string evidenceStrengthExplanation)
    {
        var merchantLabel = string.IsNullOrWhiteSpace(input.BranchName)
            ? input.MerchantName
            : $"{input.MerchantName} ({input.BranchName})";

        var amountSentence = string.Create(
            CultureInfo.InvariantCulture,
            $"The recorded displayed or quoted price was {FormatMoney(input.CurrencyCode, input.QuotedAmount)}, while the recorded charged amount was {FormatMoney(input.CurrencyCode, input.ChargedAmount)}, creating a difference of {FormatMoney(input.CurrencyCode, input.DifferenceAmount)}.");

        var percentageSentence = input.PercentageDifference.HasValue
            ? string.Create(
                CultureInfo.InvariantCulture,
                $" This represents a {input.PercentageDifference.Value:0.00}% change against the quoted amount.")
            : string.Empty;

        return string.Create(
            CultureInfo.InvariantCulture,
            $"This complaint pack relates to case reference {input.CaseNumber} and concerns a reported pricing discrepancy at {merchantLabel} on {input.IncidentAtUtc:dd MMM yyyy HH:mm 'UTC'}. {amountSentence}{percentageSentence} PriceProof SA currently classifies the discrepancy as {input.ClassificationLabel}. {input.Explanation} {evidenceStrengthExplanation}");
    }

    private static string FormatMoney(string currencyCode, decimal amount)
    {
        return string.Create(CultureInfo.InvariantCulture, $"{currencyCode.ToUpperInvariant()} {amount:0.00}");
    }
}
