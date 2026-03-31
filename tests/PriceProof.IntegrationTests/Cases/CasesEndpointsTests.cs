using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
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
}
