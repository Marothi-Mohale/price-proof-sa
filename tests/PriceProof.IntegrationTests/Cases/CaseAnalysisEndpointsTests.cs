using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using PriceProof.Application.Auth;
using PriceProof.Application.Cases;
using PriceProof.Application.PaymentRecords;
using PriceProof.Application.PriceCaptures;
using PriceProof.Domain.Enums;
using PriceProof.Infrastructure.Seeding;

namespace PriceProof.IntegrationTests.Cases;

public sealed class CaseAnalysisEndpointsTests : IClassFixture<PriceProofApiFactory>
{
    private readonly HttpClient _client;
    private readonly AuthSessionDto _session;

    public CaseAnalysisEndpointsTests(PriceProofApiFactory factory)
    {
        (_client, _session) = factory.CreateAuthenticatedClientAsync().GetAwaiter().GetResult();
    }

    [Fact]
    public async Task Post_analyze_should_return_conservative_card_fee_analysis()
    {
        var createdCase = await CreateCaseAsync();

        await _client.PostAsJsonAsync(
            "/price-captures",
            new CreatePriceCaptureRequest(
                createdCase.Id,
                _session.UserId,
                CaptureType.PriceTagPhoto,
                EvidenceType.Image,
                199.99m,
                "ZAR",
                "display.jpg",
                "/evidence/prices/display.jpg",
                DateTimeOffset.UtcNow.AddMinutes(-9),
                "image/jpeg",
                null,
                "Displayed shelf price was R199.99.",
                null));

        await _client.PostAsJsonAsync(
            "/payment-records",
            new CreatePaymentRecordRequest(
                createdCase.Id,
                _session.UserId,
                PaymentMethod.DebitCard,
                209.99m,
                "ZAR",
                DateTimeOffset.UtcNow.AddMinutes(-6),
                "POS-ANA-001",
                null,
                "1234",
                "Merchant said a card surcharge applied."));

        var response = await _client.PostAsJsonAsync(
            $"/cases/{createdCase.Id}/analyze",
            new AnalyzeCaseRequest(MerchantSaidCardFee: true));

        var body = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(HttpStatusCode.OK, body);

        var analysis = await response.Content.ReadFromJsonAsync<CaseAnalysisDto>();
        analysis.Should().NotBeNull();
        analysis!.QuotedAmount.Should().Be(199.99m);
        analysis.ChargedAmount.Should().Be(209.99m);
        analysis.Difference.Should().Be(10.00m);
        analysis.PercentageDifference.Should().Be(5.00m);
        analysis.Classification.Should().Be("LikelyCardSurcharge");
        analysis.Confidence.Should().Be(0.94m);
        analysis.Explanation.Should().Contain("card fee");

        var refreshedCase = await _client.GetFromJsonAsync<CaseDetailDto>($"/cases/{createdCase.Id}");
        refreshedCase.Should().NotBeNull();
        refreshedCase!.Classification.Should().Be("PotentialCardSurcharge");
    }

    [Fact]
    public async Task Post_analyze_should_reject_case_without_both_amounts()
    {
        var createdCase = await CreateCaseAsync();

        var response = await _client.PostAsJsonAsync(
            $"/cases/{createdCase.Id}/analyze",
            new AnalyzeCaseRequest(EvidenceText: "Trying to analyze too early."));

        var body = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(HttpStatusCode.Conflict, body);
        body.Should().Contain("quoted amount and a charged amount");
    }

    private async Task<CaseDetailDto> CreateCaseAsync()
    {
        var response = await _client.PostAsJsonAsync(
            "/cases",
            new CreateCaseRequest(
                _session.UserId,
                SeedData.ShopriteMerchantId,
                SeedData.ShopriteSandtonBranchId,
                "Case for discrepancy analysis endpoint testing",
                DateTimeOffset.UtcNow.AddMinutes(-15),
                "ZAR",
                "CASE-ANALYZE",
                "Endpoint analysis integration test."));

        var body = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(HttpStatusCode.Created, body);

        var createdCase = await response.Content.ReadFromJsonAsync<CaseDetailDto>();
        createdCase.Should().NotBeNull();
        return createdCase!;
    }
}
