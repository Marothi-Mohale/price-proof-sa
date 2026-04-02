using FluentAssertions;
using PriceProof.Domain.Enums;
using PriceProof.Domain.Services;

namespace PriceProof.UnitTests.ComplaintPacks;

public sealed class ComplaintSubmissionGuidanceComposerTests
{
    private readonly ComplaintSubmissionGuidanceComposer _composer = new();

    [Fact]
    public void Should_include_bank_and_banking_ombud_routes_for_card_payments()
    {
        var result = _composer.Compose(new ComplaintSubmissionGuidanceInput(
            "PP-20260402-CARD",
            "Shoprite",
            "Sandton City",
            new DateTimeOffset(2026, 4, 2, 11, 30, 0, TimeSpan.Zero),
            "ZAR",
            100m,
            110m,
            10m,
            "Likely Card Surcharge",
            ComplaintEvidenceStrength.Strong,
            PaymentMethod.CreditCard));

        result.RecommendedRoutes.Should().Contain(route => route.Channel.Contains("Bank dispute team"));
        result.RecommendedRoutes.Should().Contain(route => route.Channel.Contains("National Financial Ombud Scheme"));
        result.EmailTemplate.Subject.Should().Contain("PP-20260402-CARD");
        result.EmailTemplate.Body.Should().Contain("The recorded quoted or displayed price was ZAR 100.00.");
    }

    [Fact]
    public void Should_skip_bank_specific_routes_for_non_card_payments()
    {
        var result = _composer.Compose(new ComplaintSubmissionGuidanceInput(
            "PP-20260402-CASH",
            "Checkers",
            null,
            new DateTimeOffset(2026, 4, 2, 9, 15, 0, TimeSpan.Zero),
            "ZAR",
            55m,
            60m,
            5m,
            "Unclear Positive Mismatch",
            ComplaintEvidenceStrength.Moderate,
            PaymentMethod.Cash));

        result.RecommendedRoutes.Should().NotContain(route => route.Channel.Contains("Bank dispute team"));
        result.RecommendedRoutes.Should().NotContain(route => route.Channel.Contains("National Financial Ombud Scheme"));
        result.RecommendedRoutes.Should().Contain(route => route.Channel.Contains("CGSO"));
        result.RecommendedRoutes.Should().Contain(route => route.Channel.Contains("NCC"));
    }

    [Fact]
    public void Should_tell_user_to_disclose_limited_evidence_when_pack_is_weak()
    {
        var result = _composer.Compose(new ComplaintSubmissionGuidanceInput(
            "PP-20260402-WEAK",
            "Dis-Chem",
            null,
            new DateTimeOffset(2026, 4, 2, 7, 45, 0, TimeSpan.Zero),
            "ZAR",
            49.99m,
            59.99m,
            10m,
            "Needs Review",
            ComplaintEvidenceStrength.Weak,
            PaymentMethod.DebitCard));

        result.SafeUseNote.Should().Contain("evidence is currently limited");
        result.EmailTemplate.Body.Should().Contain("The current evidence record is limited");
        result.EmailTemplate.Body.Should().Contain("[Your full name]");
    }
}
