using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using PriceProof.Application.Cases;
using PriceProof.Application.PaymentRecords;
using PriceProof.Application.ReceiptRecords;
using PriceProof.Domain.Enums;
using PriceProof.Infrastructure.Seeding;

namespace PriceProof.IntegrationTests.ReceiptRecords;

public sealed class ReceiptOcrEndpointsTests : IClassFixture<PriceProofApiFactory>
{
    private readonly HttpClient _client;

    public ReceiptOcrEndpointsTests(PriceProofApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Post_run_ocr_should_extract_normalized_receipt_fields()
    {
        var createdCase = await CreateCaseAsync();
        var paymentRecord = await CreatePaymentRecordAsync(createdCase.Id);
        var receiptRecord = await CreateReceiptRecordAsync(createdCase.Id, paymentRecord.Id);

        var response = await _client.PostAsync($"/receipt-records/{receiptRecord.Id}/run-ocr", content: null);
        var body = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(HttpStatusCode.OK, body);

        var result = await response.Content.ReadFromJsonAsync<RunReceiptOcrResultDto>();
        result.Should().NotBeNull();
        result!.ReceiptRecordId.Should().Be(receiptRecord.Id);
        result.ProviderName.Should().Be("AzureDocumentIntelligence");
        result.Confidence.Should().BeGreaterThan(0.5m);
        result.MerchantName.Should().Be("SHOPRITE SANDTON CITY");
        result.TransactionTotal.Should().Be(49.98m);
        result.TransactionAtUtc.Should().Be(new DateTimeOffset(2026, 4, 1, 12, 45, 0, TimeSpan.Zero));
        result.LineItems.Should().HaveCount(2);
        var descriptions = result.LineItems.Select(item => item.Description).ToArray();
        descriptions.Should().Contain("MILK");
        descriptions.Should().Contain("BREAD");
        result.RawPayloadMetadataJson.Should().Contain("\"fake\":true");
        result.RawText.Should().Contain("TOTAL 49.98");
    }

    private async Task<CaseDetailDto> CreateCaseAsync()
    {
        var response = await _client.PostAsJsonAsync(
            "/cases",
            new CreateCaseRequest(
                SeedData.DemoUserId,
                SeedData.ShopriteMerchantId,
                SeedData.ShopriteSandtonBranchId,
                "Receipt OCR integration test case",
                DateTimeOffset.UtcNow.AddMinutes(-10),
                "ZAR",
                "CASE-OCR",
                "Testing receipt OCR endpoint."));

        var body = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(HttpStatusCode.Created, body);
        return (await response.Content.ReadFromJsonAsync<CaseDetailDto>())!;
    }

    private async Task<PaymentRecordDto> CreatePaymentRecordAsync(Guid caseId)
    {
        var response = await _client.PostAsJsonAsync(
            "/payment-records",
            new CreatePaymentRecordRequest(
                caseId,
                SeedData.DemoUserId,
                PaymentMethod.DebitCard,
                49.98m,
                "ZAR",
                DateTimeOffset.UtcNow.AddMinutes(-8),
                "PAY-OCR-001",
                null,
                "4321",
                "Payment for OCR receipt test."));

        var body = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(HttpStatusCode.Created, body);
        return (await response.Content.ReadFromJsonAsync<PaymentRecordDto>())!;
    }

    private async Task<ReceiptRecordDto> CreateReceiptRecordAsync(Guid caseId, Guid paymentRecordId)
    {
        var response = await _client.PostAsJsonAsync(
            "/receipt-records",
            new CreateReceiptRecordRequest(
                caseId,
                paymentRecordId,
                SeedData.DemoUserId,
                EvidenceType.Image,
                "receipt-ocr.txt",
                "text/plain",
                "/receipts/ocr/receipt-ocr.txt",
                DateTimeOffset.UtcNow.AddMinutes(-5),
                "ZAR",
                null,
                null,
                null,
                null,
                null));

        var body = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(HttpStatusCode.Created, body);
        return (await response.Content.ReadFromJsonAsync<ReceiptRecordDto>())!;
    }
}
