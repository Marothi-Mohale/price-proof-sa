using PriceProofSA.Domain.Enums;
using PriceProofSA.Domain.Services;

namespace PriceProofSA.Application.Tests;

public sealed class DiscrepancyClassifierTests
{
    private readonly DiscrepancyClassifier _classifier = new();

    [Fact]
    public void Analyze_ShouldFlagLikelyCardSurcharge_WhenCardPaymentIsHigherThanQuote()
    {
        var result = _classifier.Analyze(100m, 105m, true, "POS receipt");

        Assert.Equal(DiscrepancyClassification.LikelyCardSurcharge, result.Classification);
        Assert.True(result.LikelyUnlawfulCardSurcharge);
        Assert.Equal(5m, result.DifferenceAmount);
    }

    [Fact]
    public void Analyze_ShouldFlagPossibleCashback_WhenContextIncludesCashbackLanguage()
    {
        var result = _classifier.Analyze(150m, 200m, true, "Cashback on purchase");

        Assert.Equal(DiscrepancyClassification.PossibleCashback, result.Classification);
        Assert.False(result.LikelyUnlawfulCardSurcharge);
    }

    [Fact]
    public void Analyze_ShouldReturnNoMismatch_WhenChargedAmountIsEqualToQuotedAmount()
    {
        var result = _classifier.Analyze(79.99m, 79.99m, true);

        Assert.Equal(DiscrepancyClassification.NoMismatch, result.Classification);
        Assert.False(result.LikelyUnlawfulCardSurcharge);
    }
}
