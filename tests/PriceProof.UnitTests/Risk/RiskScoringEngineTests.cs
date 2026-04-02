using FluentAssertions;
using PriceProof.Domain.Enums;
using PriceProof.Domain.Services;

namespace PriceProof.UnitTests.Risk;

public sealed class RiskScoringEngineTests
{
    private readonly RiskScoringEngine _engine = new();
    private readonly DateTimeOffset _now = new(2026, 4, 1, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Should_return_low_zero_score_when_no_cases_exist()
    {
        var result = _engine.Calculate([], _now);

        result.Score.Should().Be(0m);
        result.Label.Should().Be(RiskLabel.Low);
        result.TotalCases.Should().Be(0);
        result.AnalyzedCases.Should().Be(0);
    }

    [Fact]
    public void Should_escalate_to_severe_for_multiple_recent_likely_surcharge_cases()
    {
        var result = _engine.Calculate(
            [
                CreateSignal(DiscrepancyAnalysisClassification.LikelyCardSurcharge, 12m, 0.95m, _now.AddDays(-4)),
                CreateSignal(DiscrepancyAnalysisClassification.LikelyCardSurcharge, 10m, 0.90m, _now.AddDays(-8)),
                CreateSignal(DiscrepancyAnalysisClassification.LikelyCardSurcharge, 8m, 0.92m, _now.AddDays(-16))
            ],
            _now);

        result.Score.Should().Be(81.94m);
        result.Label.Should().Be(RiskLabel.Severe);
        result.LikelyCardSurchargeCases.Should().Be(3);
        result.ConfidenceWeightedMismatchTotal.Should().Be(27.76m);
        result.RecencyWeightedCaseCount.Should().Be(3m);
    }

    [Fact]
    public void Should_discount_score_when_most_analyzed_cases_are_benign()
    {
        var result = _engine.Calculate(
            [
                CreateSignal(DiscrepancyAnalysisClassification.LikelyCardSurcharge, 10m, 0.90m, _now.AddDays(-5)),
                CreateSignal(DiscrepancyAnalysisClassification.Match, 0m, 0.99m, _now.AddDays(-4)),
                CreateSignal(DiscrepancyAnalysisClassification.Match, 0m, 0.99m, _now.AddDays(-3)),
                CreateSignal(DiscrepancyAnalysisClassification.Match, 0m, 0.99m, _now.AddDays(-2)),
                CreateSignal(DiscrepancyAnalysisClassification.Match, 0m, 0.99m, _now.AddDays(-1)),
                CreateSignal(DiscrepancyAnalysisClassification.LowerThanQuoted, -5m, 0.96m, _now.AddDays(-7))
            ],
            _now);

        result.Score.Should().Be(22.89m);
        result.Label.Should().Be(RiskLabel.Low);
        result.DismissedEquivalentRatio.Should().Be(0.8333m);
    }

    [Fact]
    public void Should_weight_recent_cases_more_than_old_cases()
    {
        var oldWeighted = _engine.Calculate(
            [
                CreateSignal(DiscrepancyAnalysisClassification.LikelyCardSurcharge, 20m, 0.90m, _now.AddDays(-10)),
                CreateSignal(DiscrepancyAnalysisClassification.LikelyCardSurcharge, 20m, 0.90m, _now.AddDays(-220))
            ],
            _now);

        var recentWeighted = _engine.Calculate(
            [
                CreateSignal(DiscrepancyAnalysisClassification.LikelyCardSurcharge, 20m, 0.90m, _now.AddDays(-10)),
                CreateSignal(DiscrepancyAnalysisClassification.LikelyCardSurcharge, 20m, 0.90m, _now.AddDays(-12))
            ],
            _now);

        oldWeighted.Score.Should().Be(48.07m);
        recentWeighted.Score.Should().Be(55.67m);
        recentWeighted.Score.Should().BeGreaterThan(oldWeighted.Score);
    }

    [Fact]
    public void Should_reduce_score_when_signals_are_unclear()
    {
        var result = _engine.Calculate(
            [
                CreateSignal(DiscrepancyAnalysisClassification.LikelyCardSurcharge, 10m, 0.90m, _now.AddDays(-4)),
                CreateSignal(DiscrepancyAnalysisClassification.UnclearPositiveMismatch, 10m, 0.60m, _now.AddDays(-3))
            ],
            _now);

        result.Score.Should().Be(31.88m);
        result.Label.Should().Be(RiskLabel.Moderate);
        result.UnclearCaseRatio.Should().Be(0.5000m);
    }

    private RiskCaseSignal CreateSignal(
        DiscrepancyAnalysisClassification classification,
        decimal differenceAmount,
        decimal confidence,
        DateTimeOffset analyzedAtUtc)
    {
        return new RiskCaseSignal(
            Guid.NewGuid(),
            classification,
            confidence,
            differenceAmount,
            analyzedAtUtc.AddMinutes(-15),
            analyzedAtUtc);
    }
}
