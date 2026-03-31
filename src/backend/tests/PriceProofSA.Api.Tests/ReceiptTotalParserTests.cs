using PriceProofSA.Infrastructure.Ocr;

namespace PriceProofSA.Api.Tests;

public sealed class ReceiptTotalParserTests
{
    private readonly ReceiptTotalParser _parser = new();

    [Fact]
    public void Parse_ShouldPreferLinesContainingTotal()
    {
        const string rawText = """
                               VAT 12.50
                               SUBTOTAL 102.50
                               TOTAL 115.00
                               """;

        var result = _parser.Parse(rawText);

        Assert.Equal(115.00m, result);
    }

    [Fact]
    public void Parse_ShouldReturnLargestPlausibleAmount_WhenNoTotalKeywordExists()
    {
        var result = _parser.Parse("Line item 30.00 and another line 75.00");

        Assert.Equal(75.00m, result);
    }
}
