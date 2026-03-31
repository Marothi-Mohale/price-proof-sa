using System.Globalization;
using PriceProofSA.Domain.Enums;
using PriceProofSA.Domain.ValueObjects;

namespace PriceProofSA.Domain.Services;

public sealed class DiscrepancyClassifier
{
    public DiscrepancyAnalysis Analyze(decimal quotedAmount, decimal chargedAmount, bool isCardPayment, params string?[] contextNotes)
    {
        var difference = Math.Round(chargedAmount - quotedAmount, 2, MidpointRounding.AwayFromZero);
        if (difference <= 0)
        {
            return new DiscrepancyAnalysis(
                quotedAmount,
                chargedAmount,
                difference,
                DiscrepancyClassification.NoMismatch,
                false,
                "Quoted and charged amounts match or the customer was charged less than quoted.");
        }

        var combinedContext = string.Join(" ", contextNotes.Where(static note => !string.IsNullOrWhiteSpace(note)))
            .ToLowerInvariant();

        if (combinedContext.Contains("cashback", StringComparison.Ordinal) ||
            combinedContext.Contains("cash back", StringComparison.Ordinal))
        {
            return Build(
                quotedAmount,
                chargedAmount,
                difference,
                DiscrepancyClassification.PossibleCashback,
                "The mismatch includes 'cashback' language, so this may reflect a customer cash withdrawal rather than a surcharge.");
        }

        if (combinedContext.Contains("service fee", StringComparison.Ordinal) ||
            combinedContext.Contains("admin fee", StringComparison.Ordinal) ||
            combinedContext.Contains("booking fee", StringComparison.Ordinal) ||
            combinedContext.Contains("handling fee", StringComparison.Ordinal))
        {
            return Build(
                quotedAmount,
                chargedAmount,
                difference,
                DiscrepancyClassification.PossibleSeparateServiceFee,
                "The mismatch appears to reference a separate service-style fee that should be reviewed against the quote and receipt wording.");
        }

        if (difference >= 50m && IsNearRoundDenomination(difference))
        {
            return Build(
                quotedAmount,
                chargedAmount,
                difference,
                DiscrepancyClassification.PossibleCashback,
                "The mismatch is a round cash-like amount, which can indicate cashback on purchase.");
        }

        if (isCardPayment && difference > 0 && difference <= Math.Max(50m, Math.Round(quotedAmount * 0.10m, 2)))
        {
            var explanation =
                $"The charged amount exceeds the quoted amount by R{difference.ToString("0.00", CultureInfo.InvariantCulture)} on a card payment with no clearer explanation.";

            return new DiscrepancyAnalysis(
                quotedAmount,
                chargedAmount,
                difference,
                DiscrepancyClassification.LikelyCardSurcharge,
                true,
                explanation);
        }

        return Build(
            quotedAmount,
            chargedAmount,
            difference,
            DiscrepancyClassification.Unclear,
            "A mismatch exists, but the available evidence is insufficient to confidently classify the reason.");
    }

    private static DiscrepancyAnalysis Build(
        decimal quotedAmount,
        decimal chargedAmount,
        decimal difference,
        DiscrepancyClassification classification,
        string explanation)
    {
        return new DiscrepancyAnalysis(
            quotedAmount,
            chargedAmount,
            difference,
            classification,
            classification == DiscrepancyClassification.LikelyCardSurcharge,
            explanation);
    }

    private static bool IsNearRoundDenomination(decimal difference)
    {
        return difference % 50m == 0m || difference % 100m == 0m;
    }
}
