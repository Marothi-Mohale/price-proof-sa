using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using PriceProof.Application.Abstractions.Ocr;
using PriceProof.Infrastructure.Ocr;
using PriceProof.Infrastructure.Options;

namespace PriceProof.UnitTests.ReceiptRecords;

public sealed class ReceiptOcrNormalizationTests
{
    [Fact]
    public async Task Should_normalize_raw_text_into_common_receipt_fields()
    {
        var orchestrator = CreateOrchestrator(new FakeOcrProvider(new OcrProviderResult(
            "GoogleVision",
            true,
            """
            SHOPRITE SANDTON CITY
            2026-03-31 15:45
            MILK 29.99
            BREAD 19.99
            TOTAL 49.98
            """,
            "{\"provider\":\"fake\"}")));

        var result = await orchestrator.RecognizeReceiptAsync(new OcrDocumentContent("receipt.txt", "text/plain", []));

        result.ProviderName.Should().Be("GoogleVision");
        result.MerchantName.Should().Be("SHOPRITE SANDTON CITY");
        result.TransactionTotal.Should().Be(49.98m);
        result.TransactionAtUtc.Should().Be(new DateTimeOffset(2026, 3, 31, 15, 45, 0, TimeSpan.Zero));
        result.LineItems.Should().ContainSingle(item => item.Description == "MILK" && item.TotalAmount == 29.99m);
        result.LineItems.Should().ContainSingle(item => item.Description == "BREAD" && item.TotalAmount == 19.99m);
        result.Confidence.Should().Be(0.95m);
    }

    [Fact]
    public async Task Should_preserve_provider_structured_fields_and_filter_blank_line_items()
    {
        var orchestrator = CreateOrchestrator(new FakeOcrProvider(new OcrProviderResult(
            "AzureDocumentIntelligence",
            true,
            "fallback text",
            "{\"provider\":\"fake\"}",
            Confidence: 1.2m,
            MerchantName: "Dis-Chem Rosebank Mall",
            TransactionTotal: 149.99m,
            TransactionAtUtc: new DateTimeOffset(2026, 4, 1, 9, 30, 0, TimeSpan.Zero),
            LineItems:
            [
                new OcrLineItem("", 149.99m),
                new OcrLineItem("Pain relief", 149.99m, 1m, 149.99m)
            ],
            ReceiptNumber: "RCPT-9001")));

        var result = await orchestrator.RecognizeReceiptAsync(new OcrDocumentContent("receipt.txt", "text/plain", []));

        result.MerchantName.Should().Be("Dis-Chem Rosebank Mall");
        result.TransactionTotal.Should().Be(149.99m);
        result.TransactionAtUtc.Should().Be(new DateTimeOffset(2026, 4, 1, 9, 30, 0, TimeSpan.Zero));
        result.LineItems.Should().ContainSingle(item => item.Description == "Pain relief");
        result.ReceiptNumber.Should().Be("RCPT-9001");
        result.Confidence.Should().Be(1.0m);
    }

    private static OcrOrchestrator CreateOrchestrator(params IOcrProvider[] providers)
    {
        return new OcrOrchestrator(
            providers,
            new ReceiptOcrTextParser(),
            Options.Create(new OcrOptions
            {
                Enabled = true,
                PrimaryProvider = providers[0].Name,
                RetryCount = 0,
                RequestTimeoutSeconds = 5
            }),
            NullLogger<OcrOrchestrator>.Instance);
    }

    private sealed class FakeOcrProvider : IOcrProvider
    {
        private readonly OcrProviderResult _result;

        public FakeOcrProvider(OcrProviderResult result)
        {
            _result = result;
        }

        public string Name => _result.ProviderName;

        public bool IsConfigured => true;

        public Task<OcrProviderResult> RecognizeAsync(OcrDocumentContent document, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_result);
        }
    }
}
