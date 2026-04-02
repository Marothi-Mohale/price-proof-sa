using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using PriceProof.Application.Abstractions.Diagnostics;
using PriceProof.Application.Cases;
using PriceProof.Infrastructure.Seeding;

namespace PriceProof.IntegrationTests.Cases;

public sealed class CasesEndpointsTests : IClassFixture<PriceProofApiFactory>
{
    private readonly HttpClient _client;

    public CasesEndpointsTests(PriceProofApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Post_case_then_get_case_should_round_trip()
    {
        var request = new CreateCaseRequest(
            SeedData.DemoUserId,
            SeedData.ShopriteMerchantId,
            SeedData.ShopriteSandtonBranchId,
            "Basket of grocery items bought after a shelf price dispute",
            DateTimeOffset.UtcNow.AddMinutes(-15),
            "ZAR",
            "CASE-001",
            "Customer alleges a card surcharge at checkout.");

        var createResponse = await _client.PostAsJsonAsync("/cases", request);
        var createBody = await createResponse.Content.ReadAsStringAsync();
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created, createBody);

        var createdCase = await createResponse.Content.ReadFromJsonAsync<CaseDetailDto>();
        createdCase.Should().NotBeNull();
        createdCase!.Merchant.Id.Should().Be(SeedData.ShopriteMerchantId);
        createdCase.Branch!.Id.Should().Be(SeedData.ShopriteSandtonBranchId);
        createdCase.Status.Should().NotBeNullOrWhiteSpace();

        var getResponse = await _client.GetAsync($"/cases/{createdCase.Id}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var fetchedCase = await getResponse.Content.ReadFromJsonAsync<CaseDetailDto>();
        fetchedCase.Should().NotBeNull();
        fetchedCase!.Id.Should().Be(createdCase.Id);
        fetchedCase.CaseNumber.Should().Be(createdCase.CaseNumber);
    }

    [Fact]
    public async Task Post_case_should_echo_correlation_id_and_store_it_in_audit_log()
    {
        const string correlationId = "integration-case-correlation";
        using var request = new HttpRequestMessage(HttpMethod.Post, "/cases")
        {
            Content = JsonContent.Create(new CreateCaseRequest(
                SeedData.DemoUserId,
                SeedData.ShopriteMerchantId,
                SeedData.ShopriteSandtonBranchId,
                "Correlation check basket",
                DateTimeOffset.UtcNow.AddMinutes(-10),
                "ZAR",
                "CASE-CORRELATION",
                "Correlation integration test."))
        };
        request.Headers.Add(RequestContextConstants.CorrelationIdHeaderName, correlationId);

        var createResponse = await _client.SendAsync(request);
        var createBody = await createResponse.Content.ReadAsStringAsync();
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created, createBody);
        createResponse.Headers.GetValues(RequestContextConstants.CorrelationIdHeaderName).Should().ContainSingle(correlationId);

        var createdCase = await createResponse.Content.ReadFromJsonAsync<CaseDetailDto>();
        createdCase.Should().NotBeNull();

        var fetchedCase = await _client.GetFromJsonAsync<CaseDetailDto>($"/cases/{createdCase!.Id}");
        fetchedCase.Should().NotBeNull();
        fetchedCase!.AuditLogs.Should().Contain(log => log.Action == "CaseCreated" && log.CorrelationId == correlationId);
    }

    [Fact]
    public async Task Post_case_with_custom_merchant_should_create_a_new_merchant_record()
    {
        var request = new CreateCaseRequest(
            SeedData.DemoUserId,
            null,
            null,
            "Basic grocery purchase from a local shop",
            DateTimeOffset.UtcNow.AddMinutes(-12),
            "ZAR",
            "CASE-CUSTOM",
            "User reported an unlisted neighbourhood merchant.",
            "Corner Supermarket");

        var response = await _client.PostAsJsonAsync("/cases", request);
        var body = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(HttpStatusCode.Created, body);

        var createdCase = await response.Content.ReadFromJsonAsync<CaseDetailDto>();
        createdCase.Should().NotBeNull();
        createdCase!.Merchant.Name.Should().Be("Corner Supermarket");
        createdCase.Branch.Should().BeNull();
        createdCase.AuditLogs.Should().Contain(log => log.Action == "MerchantCreatedFromCaseIntake");
    }

    [Fact]
    public async Task Post_case_with_custom_merchant_matching_existing_name_should_reuse_existing_merchant()
    {
        var request = new CreateCaseRequest(
            SeedData.DemoUserId,
            null,
            null,
            "Existing merchant reuse",
            DateTimeOffset.UtcNow.AddMinutes(-6),
            "ZAR",
            "CASE-REUSE",
            "User typed a known merchant name instead of selecting it.",
            "  shoprite  ");

        var response = await _client.PostAsJsonAsync("/cases", request);
        var body = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(HttpStatusCode.Created, body);

        var createdCase = await response.Content.ReadFromJsonAsync<CaseDetailDto>();
        createdCase.Should().NotBeNull();
        createdCase!.Merchant.Id.Should().Be(SeedData.ShopriteMerchantId);
        createdCase.Merchant.Name.Should().Be("Shoprite");
        createdCase.AuditLogs.Should().NotContain(log => log.Action == "MerchantCreatedFromCaseIntake");
    }
}
