using PriceProofSA.Domain.Services;

namespace PriceProofSA.Application.Tests;

public sealed class MerchantRiskCalculatorTests
{
    private readonly MerchantRiskCalculator _calculator = new();

    [Fact]
    public void Calculate_ShouldReturnCriticalTrend_ForHighVolumeAndConfirmedSignals()
    {
        var result = _calculator.Calculate(8, 6);

        Assert.Equal("Critical", result.Trend);
        Assert.True(result.Score >= 75m);
    }

    [Fact]
    public void Calculate_ShouldReturnLowTrend_WhenThereAreNoReports()
    {
        var result = _calculator.Calculate(0, 0);

        Assert.Equal("Low", result.Trend);
        Assert.Equal(0m, result.Score);
    }
}
