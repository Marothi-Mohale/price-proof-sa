using PriceProofSA.Application.Abstractions.Ocr;
using PriceProofSA.Infrastructure.Ocr;

namespace PriceProofSA.Api.Tests;

public sealed class MockOcrProviderTests
{
    private readonly MockOcrProvider _provider = new(new ReceiptTotalParser());

    [Fact]
    public async Task TryRecognizeAsync_ShouldInferAmountFromFileName()
    {
        var result = await _provider.TryRecognizeAsync(
            new OcrDocumentRequest("receipt-199.99.jpg", "image/jpeg", []));

        Assert.True(result.Success);
        Assert.Equal(199.99m, result.ParsedAmount);
    }
}
