using PriceProofSA.Application.Services;

namespace PriceProofSA.Application.Tests;

public sealed class BankNotificationRedactorTests
{
    [Fact]
    public void Redact_ShouldMaskLongDigitSequences()
    {
        var result = BankNotificationRedactor.Redact("Card purchase 123456789012 at Merchant");

        Assert.Equal("Card purchase ********9012 at Merchant", result);
    }
}
