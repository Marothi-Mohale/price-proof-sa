using FluentAssertions;
using PriceProof.Domain.Enums;
using PriceProof.Domain.Services;

namespace PriceProof.UnitTests.Cases;

public sealed class DiscrepancyDetectionEngineTests
{
    private readonly DiscrepancyDetectionEngine _engine = new();

    [Fact]
    public void Should_classify_match_when_amounts_are_equal_after_rounding()
    {
        var result = _engine.Analyze(new DiscrepancyAnalysisInput(100.005m, 100.006m, "zar"));

        result.Classification.Should().Be(DiscrepancyAnalysisClassification.Match);
        result.Difference.Should().Be(0.00m);
        result.PercentageDifference.Should().Be(0.00m);
        result.Confidence.Should().Be(0.99m);
    }

    [Fact]
    public void Should_classify_likely_card_surcharge_when_user_marked_card_fee()
    {
        var result = _engine.Analyze(new DiscrepancyAnalysisInput(
            100m,
            105m,
            "ZAR",
            MerchantSaidCardFee: true,
            CashbackPresent: true,
            DeliveryOrServiceFeePresent: true));

        result.Classification.Should().Be(DiscrepancyAnalysisClassification.LikelyCardSurcharge);
        result.Confidence.Should().Be(0.94m);
        result.Explanation.Should().Contain("card fee");
    }

    [Theory]
    [InlineData("Merchant added a card surcharge at the till.")]
    [InlineData("Customer was told there is a debit card fee.")]
    [InlineData("Slip says swipe fee included.")]
    public void Should_detect_card_fee_from_evidence_text(string evidenceText)
    {
        var result = _engine.Analyze(new DiscrepancyAnalysisInput(79.99m, 85.99m, "ZAR", EvidenceText: evidenceText));

        result.Classification.Should().Be(DiscrepancyAnalysisClassification.LikelyCardSurcharge);
        result.Confidence.Should().Be(0.86m);
    }

    [Fact]
    public void Should_classify_possible_cashback_when_cashback_was_marked()
    {
        var result = _engine.Analyze(new DiscrepancyAnalysisInput(
            50m,
            150m,
            "ZAR",
            CashbackPresent: true,
            EvidenceText: "Customer requested R100 cashback."));

        result.Classification.Should().Be(DiscrepancyAnalysisClassification.PossibleCashback);
        result.PercentageDifference.Should().Be(200.00m);
        result.Explanation.Should().Contain("Cashback");
    }

    [Fact]
    public void Should_classify_possible_separate_fee_when_service_fee_was_marked()
    {
        var result = _engine.Analyze(new DiscrepancyAnalysisInput(
            200m,
            225m,
            "ZAR",
            DeliveryOrServiceFeePresent: true,
            EvidenceText: "Courier service fee may apply."));

        result.Classification.Should().Be(DiscrepancyAnalysisClassification.PossibleSeparateFee);
        result.Explanation.Should().Contain("service fee");
    }

    [Fact]
    public void Should_fall_back_to_unclear_positive_mismatch_without_explanatory_signals()
    {
        var result = _engine.Analyze(new DiscrepancyAnalysisInput(39.99m, 44.99m, "ZAR", EvidenceText: "Paid at the till."));

        result.Classification.Should().Be(DiscrepancyAnalysisClassification.UnclearPositiveMismatch);
        result.Confidence.Should().Be(0.58m);
        result.Difference.Should().Be(5.00m);
    }

    [Fact]
    public void Should_classify_lower_than_quoted_when_charged_amount_is_lower()
    {
        var result = _engine.Analyze(new DiscrepancyAnalysisInput(120m, 99.99m, "ZAR"));

        result.Classification.Should().Be(DiscrepancyAnalysisClassification.LowerThanQuoted);
        result.Difference.Should().Be(-20.01m);
        result.PercentageDifference.Should().Be(-16.68m);
    }

    [Fact]
    public void Should_return_null_percentage_difference_when_quoted_amount_is_zero()
    {
        var result = _engine.Analyze(new DiscrepancyAnalysisInput(0m, 15m, "ZAR"));

        result.Classification.Should().Be(DiscrepancyAnalysisClassification.UnclearPositiveMismatch);
        result.PercentageDifference.Should().BeNull();
        result.Explanation.Should().Contain("ZAR 15.00");
    }

    [Fact]
    public void Should_not_treat_generic_service_fee_text_as_card_surcharge()
    {
        var result = _engine.Analyze(new DiscrepancyAnalysisInput(
            99.99m,
            109.99m,
            "ZAR",
            EvidenceText: "A service fee was applied for delivery."));

        result.Classification.Should().Be(DiscrepancyAnalysisClassification.UnclearPositiveMismatch);
    }

    [Fact]
    public void Should_allow_custom_rule_sets_for_future_extensions()
    {
        var engine = new DiscrepancyDetectionEngine([new CustomRule()]);

        var result = engine.Analyze(new DiscrepancyAnalysisInput(100m, 101m, "ZAR"));

        result.Classification.Should().Be(DiscrepancyAnalysisClassification.PossibleSeparateFee);
        result.Explanation.Should().Be("Custom classifier executed.");
    }

    private sealed class CustomRule : IDiscrepancyAnalysisRule
    {
        public bool TryEvaluate(DiscrepancyDetectionEngine.DiscrepancyAnalysisContext context, out DiscrepancyAnalysisResult result)
        {
            result = new DiscrepancyAnalysisResult(
                context.QuotedAmount,
                context.ChargedAmount,
                context.Difference,
                context.PercentageDifference,
                DiscrepancyAnalysisClassification.PossibleSeparateFee,
                0.61m,
                "Custom classifier executed.");

            return true;
        }
    }
}
