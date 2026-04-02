using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using PriceProof.Application.Auth;
using PriceProof.Application.Cases;
using PriceProof.Application.PaymentRecords;
using PriceProof.Application.PriceCaptures;
using PriceProof.Application.Risk;
using PriceProof.Domain.Enums;
using PriceProof.Infrastructure.Seeding;

namespace PriceProof.IntegrationTests.Risk;

public sealed class RiskEndpointsTests : IClassFixture<PriceProofApiFactory>
{
    private readonly PriceProofApiFactory _factory;
    private readonly HttpClient _client;
    private readonly AuthSessionDto _session;

    public RiskEndpointsTests(PriceProofApiFactory factory)
    {
        _factory = factory;
        (_client, _session) = factory.CreateAuthenticatedClientAsync().GetAwaiter().GetResult();
    }

    [Fact]
    public async Task Analyze_should_recalculate_merchant_and_branch_risk()
    {
        var createdCase = await CreateCaseAsync();

        await AddQuotedAndPaidEvidenceAsync(createdCase.Id);
        await AnalyzeAsync(createdCase.Id);

        var merchantResponse = await _client.GetAsync($"/merchants/{SeedData.ShopriteMerchantId}/risk");
        var merchantBody = await merchantResponse.Content.ReadAsStringAsync();
        merchantResponse.StatusCode.Should().Be(HttpStatusCode.OK, merchantBody);

        var merchantRisk = await merchantResponse.Content.ReadFromJsonAsync<MerchantRiskDto>();
        merchantRisk.Should().NotBeNull();
        merchantRisk!.MerchantId.Should().Be(SeedData.ShopriteMerchantId);
        merchantRisk.Score.Should().BeGreaterThan(0m);
        merchantRisk.Label.Should().NotBeNullOrWhiteSpace();
        merchantRisk.Snapshots.Should().NotBeEmpty();

        var branchResponse = await _client.GetAsync($"/branches/{SeedData.ShopriteSandtonBranchId}/risk");
        var branchBody = await branchResponse.Content.ReadAsStringAsync();
        branchResponse.StatusCode.Should().Be(HttpStatusCode.OK, branchBody);

        var branchRisk = await branchResponse.Content.ReadFromJsonAsync<BranchRiskDto>();
        branchRisk.Should().NotBeNull();
        branchRisk!.BranchId.Should().Be(SeedData.ShopriteSandtonBranchId);
        branchRisk.Score.Should().BeGreaterThan(0m);
        branchRisk.Snapshots.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Overview_should_require_admin_access()
    {
        var response = await _client.GetAsync("/risk/overview");
        var body = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden, body);
    }

    [Fact]
    public async Task Overview_should_return_ranked_entities_for_admin_users()
    {
        var createdCase = await CreateCaseAsync();

        await AddQuotedAndPaidEvidenceAsync(createdCase.Id);
        await AnalyzeAsync(createdCase.Id);

        var (adminClient, _) = await _factory.CreateAuthenticatedClientAsync(admin: true);

        var response = await adminClient.GetAsync("/risk/overview");
        var body = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(HttpStatusCode.OK, body);

        var overview = await response.Content.ReadFromJsonAsync<RiskOverviewDto>();
        overview.Should().NotBeNull();
        overview!.TopMerchants.Should().Contain(item => item.MerchantId == SeedData.ShopriteMerchantId);
        overview.TopBranches.Should().Contain(item => item.BranchId == SeedData.ShopriteSandtonBranchId);
    }

    private async Task<CaseDetailDto> CreateCaseAsync()
    {
        var response = await _client.PostAsJsonAsync(
            "/cases",
            new CreateCaseRequest(
                _session.UserId,
                SeedData.ShopriteMerchantId,
                SeedData.ShopriteSandtonBranchId,
                "Risk scoring integration scenario",
                DateTimeOffset.UtcNow.AddMinutes(-20),
                "ZAR",
                "RISK-001",
                "Risk integration test case."));

        var body = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(HttpStatusCode.Created, body);

        var createdCase = await response.Content.ReadFromJsonAsync<CaseDetailDto>();
        createdCase.Should().NotBeNull();
        return createdCase!;
    }

    private async Task AddQuotedAndPaidEvidenceAsync(Guid caseId)
    {
        await _client.PostAsJsonAsync(
            "/price-captures",
            new CreatePriceCaptureRequest(
                caseId,
                _session.UserId,
                CaptureType.PriceTagPhoto,
                EvidenceType.Image,
                100m,
                "ZAR",
                "display.jpg",
                "/evidence/prices/display.jpg",
                DateTimeOffset.UtcNow.AddMinutes(-18),
                "image/jpeg",
                null,
                "Quoted shelf price captured before payment.",
                null));

        await _client.PostAsJsonAsync(
            "/payment-records",
            new CreatePaymentRecordRequest(
                caseId,
                _session.UserId,
                PaymentMethod.CreditCard,
                112m,
                "ZAR",
                DateTimeOffset.UtcNow.AddMinutes(-16),
                "POS-RISK-001",
                null,
                "4567",
                "Merchant stated that a card fee applied."));
    }

    private async Task AnalyzeAsync(Guid caseId)
    {
        var response = await _client.PostAsJsonAsync(
            $"/cases/{caseId}/analyze",
            new AnalyzeCaseRequest(MerchantSaidCardFee: true));

        var body = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(HttpStatusCode.OK, body);
    }
}
