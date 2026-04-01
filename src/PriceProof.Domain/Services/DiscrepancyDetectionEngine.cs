using System.Globalization;
using PriceProof.Domain.Enums;

namespace PriceProof.Domain.Services;

public sealed class DiscrepancyDetectionEngine : IDiscrepancyDetectionEngine
{
    private readonly IReadOnlyList<IDiscrepancyAnalysisRule> _rules;

    public DiscrepancyDetectionEngine()
        : this([
            new MatchRule(),
            new LikelyCardSurchargeRule(),
            new PossibleCashbackRule(),
            new PossibleSeparateFeeRule(),
            new UnclearPositiveMismatchRule(),
            new LowerThanQuotedRule()
        ])
    {
    }

    public DiscrepancyDetectionEngine(IEnumerable<IDiscrepancyAnalysisRule> rules)
    {
        _rules = rules.ToArray();

        if (_rules.Count == 0)
        {
            throw new ArgumentException("At least one discrepancy analysis rule must be configured.", nameof(rules));
        }
    }

    public DiscrepancyAnalysisResult Analyze(DiscrepancyAnalysisInput input)
    {
        var context = DiscrepancyAnalysisContext.Create(input);

        foreach (var rule in _rules)
        {
            if (rule.TryEvaluate(context, out var result))
            {
                return result;
            }
        }

        throw new InvalidOperationException("No discrepancy analysis rule matched the supplied input.");
    }

    public sealed record DiscrepancyAnalysisContext(
        decimal QuotedAmount,
        decimal ChargedAmount,
        decimal Difference,
        decimal? PercentageDifference,
        string CurrencyCode,
        bool MerchantSaidCardFee,
        bool CashbackPresent,
        bool DeliveryOrServiceFeePresent,
        string EvidenceText,
        bool EvidenceSuggestsCardFee)
    {
        public bool IsMatch => Difference == 0m;

        public bool IsPositiveMismatch => Difference > 0m;

        public bool IsLowerThanQuoted => Difference < 0m;

        public static DiscrepancyAnalysisContext Create(DiscrepancyAnalysisInput input)
        {
            var quotedAmount = NormalizeMoney(input.QuotedAmount);
            var chargedAmount = NormalizeMoney(input.ChargedAmount);
            var difference = NormalizeMoney(chargedAmount - quotedAmount);
            var evidenceText = NormalizeText(input.EvidenceText);
            decimal? percentageDifference = quotedAmount == 0m
                ? null
                : decimal.Round((difference / quotedAmount) * 100m, 2, MidpointRounding.AwayFromZero);

            return new DiscrepancyAnalysisContext(
                quotedAmount,
                chargedAmount,
                difference,
                percentageDifference,
                NormalizeCurrency(input.CurrencyCode),
                input.MerchantSaidCardFee,
                input.CashbackPresent,
                input.DeliveryOrServiceFeePresent,
                evidenceText,
                EvidenceTextSuggestsCardFee(evidenceText));
        }

        public string DescribeDifference()
        {
            var amountText = FormatAmount(Math.Abs(Difference), CurrencyCode);

            if (Difference == 0m)
            {
                return $"The charged amount matches the quoted amount at {FormatAmount(QuotedAmount, CurrencyCode)}.";
            }

            var percentageText = PercentageDifference.HasValue
                ? $" ({Math.Abs(PercentageDifference.Value).ToString("0.##", CultureInfo.InvariantCulture)}%)"
                : string.Empty;

            return Difference > 0m
                ? $"The charged amount is {amountText}{percentageText} above the quoted amount."
                : $"The charged amount is {amountText}{percentageText} below the quoted amount.";
        }

        private static decimal NormalizeMoney(decimal value)
        {
            return decimal.Round(value, 2, MidpointRounding.AwayFromZero);
        }

        private static string NormalizeCurrency(string currencyCode)
        {
            var normalized = string.IsNullOrWhiteSpace(currencyCode) ? "ZAR" : currencyCode.Trim().ToUpperInvariant();
            return normalized.Length > 3 ? normalized[..3] : normalized;
        }

        private static string NormalizeText(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }

        private static bool EvidenceTextSuggestsCardFee(string evidenceText)
        {
            if (string.IsNullOrWhiteSpace(evidenceText))
            {
                return false;
            }

            var text = evidenceText.ToLowerInvariant();

            string[] strongSignals =
            [
                "card fee",
                "card surcharge",
                "credit card fee",
                "credit card surcharge",
                "debit card fee",
                "debit card surcharge",
                "swipe fee",
                "swipe surcharge",
                "tap fee",
                "tap surcharge",
                "card levy",
                "card charge"
            ];

            if (strongSignals.Any(text.Contains))
            {
                return true;
            }

            var mentionsCard = text.Contains("card") || text.Contains("credit") || text.Contains("debit") || text.Contains("swipe") || text.Contains("tap");
            var mentionsExtraCharge = text.Contains("fee") || text.Contains("surcharge") || text.Contains("levy") || text.Contains("extra");

            return mentionsCard && mentionsExtraCharge;
        }

        public static string FormatAmount(decimal amount, string currencyCode)
        {
            return $"{currencyCode.ToUpperInvariant()} {amount.ToString("0.00", CultureInfo.InvariantCulture)}";
        }
    }

    private sealed class MatchRule : IDiscrepancyAnalysisRule
    {
        public bool TryEvaluate(DiscrepancyAnalysisContext context, out DiscrepancyAnalysisResult result)
        {
            if (!context.IsMatch)
            {
                result = default!;
                return false;
            }

            result = new DiscrepancyAnalysisResult(
                context.QuotedAmount,
                context.ChargedAmount,
                context.Difference,
                context.PercentageDifference,
                DiscrepancyAnalysisClassification.Match,
                0.99m,
                "The final charged amount matches the quoted or displayed amount after rounding to cents.");

            return true;
        }
    }

    private sealed class LikelyCardSurchargeRule : IDiscrepancyAnalysisRule
    {
        public bool TryEvaluate(DiscrepancyAnalysisContext context, out DiscrepancyAnalysisResult result)
        {
            if (!context.IsPositiveMismatch || (!context.MerchantSaidCardFee && !context.EvidenceSuggestsCardFee))
            {
                result = default!;
                return false;
            }

            var explanation = context.MerchantSaidCardFee
                ? $"{context.DescribeDifference()} The user marked that the merchant described the extra amount as a card fee, so this is classified as a likely card surcharge."
                : $"{context.DescribeDifference()} The evidence text suggests a card fee or surcharge, so this is classified as a likely card surcharge.";

            var confidence = context.MerchantSaidCardFee ? 0.94m : 0.86m;

            result = new DiscrepancyAnalysisResult(
                context.QuotedAmount,
                context.ChargedAmount,
                context.Difference,
                context.PercentageDifference,
                DiscrepancyAnalysisClassification.LikelyCardSurcharge,
                confidence,
                explanation);

            return true;
        }
    }

    private sealed class PossibleCashbackRule : IDiscrepancyAnalysisRule
    {
        public bool TryEvaluate(DiscrepancyAnalysisContext context, out DiscrepancyAnalysisResult result)
        {
            if (!context.IsPositiveMismatch || !context.CashbackPresent)
            {
                result = default!;
                return false;
            }

            result = new DiscrepancyAnalysisResult(
                context.QuotedAmount,
                context.ChargedAmount,
                context.Difference,
                context.PercentageDifference,
                DiscrepancyAnalysisClassification.PossibleCashback,
                0.78m,
                $"{context.DescribeDifference()} Cashback was marked as present, so the higher total may be explained by cash withdrawn during payment.");

            return true;
        }
    }

    private sealed class PossibleSeparateFeeRule : IDiscrepancyAnalysisRule
    {
        public bool TryEvaluate(DiscrepancyAnalysisContext context, out DiscrepancyAnalysisResult result)
        {
            if (!context.IsPositiveMismatch || !context.DeliveryOrServiceFeePresent)
            {
                result = default!;
                return false;
            }

            result = new DiscrepancyAnalysisResult(
                context.QuotedAmount,
                context.ChargedAmount,
                context.Difference,
                context.PercentageDifference,
                DiscrepancyAnalysisClassification.PossibleSeparateFee,
                0.74m,
                $"{context.DescribeDifference()} A delivery or service fee was marked as present, so the higher total may include a separate fee rather than a pricing mismatch.");

            return true;
        }
    }

    private sealed class UnclearPositiveMismatchRule : IDiscrepancyAnalysisRule
    {
        public bool TryEvaluate(DiscrepancyAnalysisContext context, out DiscrepancyAnalysisResult result)
        {
            if (!context.IsPositiveMismatch)
            {
                result = default!;
                return false;
            }

            result = new DiscrepancyAnalysisResult(
                context.QuotedAmount,
                context.ChargedAmount,
                context.Difference,
                context.PercentageDifference,
                DiscrepancyAnalysisClassification.UnclearPositiveMismatch,
                0.58m,
                $"{context.DescribeDifference()} There is not enough evidence yet to explain the extra amount confidently, so the mismatch remains unclear.");

            return true;
        }
    }

    private sealed class LowerThanQuotedRule : IDiscrepancyAnalysisRule
    {
        public bool TryEvaluate(DiscrepancyAnalysisContext context, out DiscrepancyAnalysisResult result)
        {
            if (!context.IsLowerThanQuoted)
            {
                result = default!;
                return false;
            }

            result = new DiscrepancyAnalysisResult(
                context.QuotedAmount,
                context.ChargedAmount,
                context.Difference,
                context.PercentageDifference,
                DiscrepancyAnalysisClassification.LowerThanQuoted,
                0.95m,
                $"{context.DescribeDifference()} The final charge is lower than the quoted amount, so this is classified as lower than quoted rather than an overcharge.");

            return true;
        }
    }
}
