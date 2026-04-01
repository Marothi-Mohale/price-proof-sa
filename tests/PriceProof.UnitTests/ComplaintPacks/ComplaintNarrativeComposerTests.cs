using FluentAssertions;
using PriceProof.Domain.Enums;
using PriceProof.Domain.Services;

namespace PriceProof.UnitTests.ComplaintPacks;

public sealed class ComplaintNarrativeComposerTests
{
    private readonly ComplaintNarrativeComposer _composer = new();

    [Fact]
    public void Should_mark_evidence_as_strong_when_quote_and_receipt_ocr_are_present()
    {
        var result = _composer.Compose(new ComplaintNarrativeInput(
            "PP-20260401-ABC",
            "Shoprite",
            "Sandton City",
            new DateTimeOffset(2026, 4, 1, 10, 15, 0, TimeSpan.Zero),
            "ZAR",
            100m,
            109.99m,
            9.99m,
            9.99m,
            "Likely Card Surcharge",
            "The merchant stated that a card fee applied.",
            HasQuotedEvidence: true,
            HasReceiptEvidence: true,
            HasReceiptOcr: true,
            EvidenceItemCount: 2));

        result.EvidenceStrength.Should().Be(ComplaintEvidenceStrength.Strong);
        result.EvidenceStrengthExplanation.Should().Contain("receipt-backed");
        result.ComplaintSummary.Should().Contain("PP-20260401-ABC");
        result.ComplaintSummary.Should().Contain("Likely Card Surcharge");
    }

    [Fact]
    public void Should_mark_evidence_as_weak_and_say_so_clearly_when_supporting_material_is_limited()
    {
        var result = _composer.Compose(new ComplaintNarrativeInput(
            "PP-20260401-WEAK",
            "Dis-Chem",
            null,
            new DateTimeOffset(2026, 4, 1, 8, 30, 0, TimeSpan.Zero),
            "ZAR",
            49.99m,
            59.99m,
            10.00m,
            20.00m,
            "Needs Review",
            "The recorded charged amount is higher than the recorded quoted amount.",
            HasQuotedEvidence: true,
            HasReceiptEvidence: false,
            HasReceiptOcr: false,
            EvidenceItemCount: 1));

        result.EvidenceStrength.Should().Be(ComplaintEvidenceStrength.Weak);
        result.EvidenceStrengthExplanation.Should().Contain("limited supporting evidence");
        result.ComplaintSummary.Should().Contain("may require additional corroboration");
        result.DeclarationText.Should().Contain("to the best of my knowledge");
    }

    [Fact]
    public void Should_include_amounts_and_percentage_in_plain_english_summary()
    {
        var result = _composer.Compose(new ComplaintNarrativeInput(
            "PP-20260401-AMOUNTS",
            "Checkers",
            "Sea Point",
            new DateTimeOffset(2026, 4, 1, 14, 45, 0, TimeSpan.Zero),
            "zar",
            200m,
            225m,
            25m,
            12.5m,
            "Possible Separate Fee",
            "The available information suggests a separate fee may have been added.",
            HasQuotedEvidence: true,
            HasReceiptEvidence: true,
            HasReceiptOcr: false,
            EvidenceItemCount: 2));

        result.ComplaintSummary.Should().Contain("ZAR 200.00");
        result.ComplaintSummary.Should().Contain("ZAR 225.00");
        result.ComplaintSummary.Should().Contain("ZAR 25.00");
        result.ComplaintSummary.Should().Contain("12.50%");
    }
}
