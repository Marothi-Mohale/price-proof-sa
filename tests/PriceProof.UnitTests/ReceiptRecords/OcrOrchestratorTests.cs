using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using PriceProof.Application.Abstractions.Ocr;
using PriceProof.Application.Common.Exceptions;
using PriceProof.Infrastructure.Ocr;
using PriceProof.Infrastructure.Options;

namespace PriceProof.UnitTests.ReceiptRecords;

public sealed class OcrOrchestratorTests
{
    [Fact]
    public async Task Should_retry_primary_provider_before_succeeding()
    {
        var primary = new FakeOcrProvider("AzureDocumentIntelligence")
            .QueueFailure("temporary", isTransient: true)
            .QueueSuccess(rawText: "SHOPRITE\nTOTAL 19.99");

        var orchestrator = CreateOrchestrator(primary);

        var result = await orchestrator.RecognizeReceiptAsync(new OcrDocumentContent("receipt.txt", "text/plain", []));

        result.ProviderName.Should().Be("AzureDocumentIntelligence");
        result.TransactionTotal.Should().Be(19.99m);
        primary.AttemptCount.Should().Be(2);
    }

    [Fact]
    public async Task Should_fall_back_to_secondary_provider_when_primary_returns_no_content()
    {
        var primary = new FakeOcrProvider("AzureDocumentIntelligence")
            .QueueFailure("primary failed", isTransient: false);
        var secondary = new FakeOcrProvider("GoogleVision")
            .QueueSuccess(rawText: "CHECKERS\nTOTAL 29.99");

        var orchestrator = CreateOrchestrator(primary, secondary);

        var result = await orchestrator.RecognizeReceiptAsync(new OcrDocumentContent("receipt.txt", "text/plain", []));

        result.ProviderName.Should().Be("GoogleVision");
        result.TransactionTotal.Should().Be(29.99m);
        secondary.AttemptCount.Should().Be(1);
    }

    [Fact]
    public async Task Should_timeout_primary_provider_and_use_secondary()
    {
        var primary = new TimeoutOcrProvider("AzureDocumentIntelligence", delay: TimeSpan.FromSeconds(2));
        var secondary = new FakeOcrProvider("GoogleVision")
            .QueueSuccess(rawText: "DIS-CHEM\n2026-04-01 15:05\nTOTAL 49.99");

        var orchestrator = CreateOrchestrator(
            new OcrOptions
            {
                Enabled = true,
                PrimaryProvider = "AzureDocumentIntelligence",
                SecondaryProvider = "GoogleVision",
                RetryCount = 0,
                RequestTimeoutSeconds = 1
            },
            primary,
            secondary);

        var result = await orchestrator.RecognizeReceiptAsync(new OcrDocumentContent("receipt.txt", "text/plain", []));

        result.ProviderName.Should().Be("GoogleVision");
        result.TransactionTotal.Should().Be(49.99m);
    }

    [Fact]
    public async Task Should_throw_safe_exception_when_no_provider_is_available()
    {
        var orchestrator = CreateOrchestrator(
            new OcrOptions
            {
                Enabled = true,
                PrimaryProvider = "AzureDocumentIntelligence",
                SecondaryProvider = "GoogleVision"
            });

        var action = () => orchestrator.RecognizeReceiptAsync(new OcrDocumentContent("receipt.txt", "text/plain", []));

        await action.Should().ThrowAsync<ServiceUnavailableException>()
            .WithMessage("Receipt OCR is currently unavailable. Please try again later.");
    }

    private static OcrOrchestrator CreateOrchestrator(params IOcrProvider[] providers)
    {
        return CreateOrchestrator(new OcrOptions
        {
            Enabled = true,
            PrimaryProvider = "AzureDocumentIntelligence",
            SecondaryProvider = "GoogleVision",
            RetryCount = 1,
            RequestTimeoutSeconds = 5
        }, providers);
    }

    private static OcrOrchestrator CreateOrchestrator(OcrOptions options, params IOcrProvider[] providers)
    {
        return new OcrOrchestrator(
            providers,
            new ReceiptOcrTextParser(),
            Options.Create(options),
            NullLogger<OcrOrchestrator>.Instance);
    }

    private sealed class FakeOcrProvider : IOcrProvider
    {
        private readonly Queue<OcrProviderResult> _results = new();

        public FakeOcrProvider(string name)
        {
            Name = name;
        }

        public string Name { get; }

        public bool IsConfigured => true;

        public int AttemptCount { get; private set; }

        public FakeOcrProvider QueueSuccess(string rawText)
        {
            _results.Enqueue(new OcrProviderResult(Name, true, rawText, "{\"fake\":true}"));
            return this;
        }

        public FakeOcrProvider QueueFailure(string message, bool isTransient)
        {
            _results.Enqueue(new OcrProviderResult(Name, false, string.Empty, "{}", FailureMessage: message, IsTransientFailure: isTransient));
            return this;
        }

        public Task<OcrProviderResult> RecognizeAsync(OcrDocumentContent document, CancellationToken cancellationToken = default)
        {
            AttemptCount++;
            return Task.FromResult(_results.Count > 0
                ? _results.Dequeue()
                : new OcrProviderResult(Name, false, string.Empty, "{}", FailureMessage: "No queued result.", IsTransientFailure: false));
        }
    }

    private sealed class TimeoutOcrProvider : IOcrProvider
    {
        private readonly TimeSpan _delay;

        public TimeoutOcrProvider(string name, TimeSpan delay)
        {
            Name = name;
            _delay = delay;
        }

        public string Name { get; }

        public bool IsConfigured => true;

        public async Task<OcrProviderResult> RecognizeAsync(OcrDocumentContent document, CancellationToken cancellationToken = default)
        {
            await Task.Delay(_delay, cancellationToken);
            return new OcrProviderResult(Name, true, "LATE RESULT", "{\"late\":true}");
        }
    }
}
