using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using PriceProof.Application.Cases;
using PriceProof.Application.ComplaintPacks;
using PriceProof.Application.PaymentRecords;
using PriceProof.Application.PriceCaptures;
using PriceProof.Application.ReceiptRecords;
using PriceProof.Application.Risk;
using PriceProof.Application.Uploads;
using PriceProof.Domain.Enums;
using PriceProof.Infrastructure.Seeding;

namespace PriceProof.IntegrationTests.Flows;

public sealed class MainApiFlowTests : IClassFixture<PriceProofApiFactory>
{
    private static readonly byte[] TinyPngBytes = Convert.FromBase64String(
        "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAusB9WlH0n0AAAAASUVORK5CYII=");

    private readonly HttpClient _client;

    public MainApiFlowTests(PriceProofApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Main_case_flow_should_create_analyze_and_generate_outputs()
    {
        var createdCase = await CreateCaseAsync();
        var quotedUpload = await UploadAsync("quoted-price.png", "image/png", TinyPngBytes, "quoted evidence");
        var receiptUpload = await UploadAsync("receipt.png", "image/png", TinyPngBytes, "receipt evidence");

        var captureResponse = await _client.PostAsJsonAsync(
            "/price-captures",
            new CreatePriceCaptureRequest(
                createdCase.Id,
                SeedData.DemoUserId,
                CaptureType.PriceTagPhoto,
                EvidenceType.Image,
                49.99m,
                "ZAR",
                quotedUpload.FileName,
                quotedUpload.StoragePath,
                DateTimeOffset.UtcNow.AddMinutes(-8),
                quotedUpload.ContentType,
                quotedUpload.ContentHash,
                "Shelf label showed 49.99.",
                "Main flow upload test."));

        captureResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var paymentResponse = await _client.PostAsJsonAsync(
            "/payment-records",
            new CreatePaymentRecordRequest(
                createdCase.Id,
                SeedData.DemoUserId,
                PaymentMethod.CreditCard,
                54.99m,
                "ZAR",
                DateTimeOffset.UtcNow.AddMinutes(-6),
                "POS-MAIN-001",
                null,
                "4242",
                "Cashier said a card fee was applied."));

        paymentResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var payment = await paymentResponse.Content.ReadFromJsonAsync<PaymentRecordDto>();
        payment.Should().NotBeNull();

        var receiptResponse = await _client.PostAsJsonAsync(
            "/receipt-records",
            new CreateReceiptRecordRequest(
                createdCase.Id,
                payment!.Id,
                SeedData.DemoUserId,
                EvidenceType.Image,
                receiptUpload.FileName,
                receiptUpload.ContentType,
                receiptUpload.StoragePath,
                DateTimeOffset.UtcNow.AddMinutes(-5),
                "ZAR",
                null,
                null,
                null,
                null,
                receiptUpload.ContentHash));

        receiptResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var receipt = await receiptResponse.Content.ReadFromJsonAsync<ReceiptRecordDto>();
        receipt.Should().NotBeNull();

        var ocrResponse = await _client.PostAsync($"/receipt-records/{receipt!.Id}/run-ocr", null);
        var ocrBody = await ocrResponse.Content.ReadAsStringAsync();
        ocrResponse.StatusCode.Should().Be(HttpStatusCode.OK, ocrBody);

        var ocr = await ocrResponse.Content.ReadFromJsonAsync<RunReceiptOcrResultDto>();
        ocr.Should().NotBeNull();
        ocr!.MerchantName.Should().Be("SHOPRITE SANDTON CITY");
        ocr.TransactionTotal.Should().Be(49.98m);

        var analysisResponse = await _client.PostAsJsonAsync(
            $"/cases/{createdCase.Id}/analyze",
            new AnalyzeCaseRequest(MerchantSaidCardFee: true));

        var analysisBody = await analysisResponse.Content.ReadAsStringAsync();
        analysisResponse.StatusCode.Should().Be(HttpStatusCode.OK, analysisBody);

        var analysis = await analysisResponse.Content.ReadFromJsonAsync<CaseAnalysisDto>();
        analysis.Should().NotBeNull();
        analysis!.Classification.Should().Be("LikelyCardSurcharge");
        analysis.Difference.Should().Be(5.00m);

        var complaintResponse = await _client.PostAsync($"/cases/{createdCase.Id}/generate-complaint-pack", null);
        var complaintBody = await complaintResponse.Content.ReadAsStringAsync();
        complaintResponse.StatusCode.Should().Be(HttpStatusCode.Created, complaintBody);

        var complaintPack = await complaintResponse.Content.ReadFromJsonAsync<GeneratedComplaintPackDto>();
        complaintPack.Should().NotBeNull();

        var merchantRisk = await _client.GetFromJsonAsync<MerchantRiskDto>($"/merchants/{SeedData.ShopriteMerchantId}/risk");
        merchantRisk.Should().NotBeNull();
        merchantRisk!.Score.Should().BeGreaterThan(0m);

        var refreshedCase = await _client.GetFromJsonAsync<CaseDetailDto>($"/cases/{createdCase.Id}");
        refreshedCase.Should().NotBeNull();
        refreshedCase!.Status.Should().Be("ReadyForReview");
        refreshedCase.Classification.Should().Be("PotentialCardSurcharge");
        refreshedCase.ComplaintPacks.Should().ContainSingle();
        refreshedCase.AuditLogs.Should().Contain(log => log.Action == "CaseCreated");
        refreshedCase.AuditLogs.Should().Contain(log => log.Action == "PriceCaptureCreated");
        refreshedCase.AuditLogs.Should().Contain(log => log.Action == "PaymentRecordCreated");
        refreshedCase.AuditLogs.Should().Contain(log => log.Action == "ReceiptRecordCreated");
        refreshedCase.AuditLogs.Should().Contain(log => log.Action == "ReceiptOcrCompleted");
        refreshedCase.AuditLogs.Should().Contain(log => log.Action == "CaseAnalyzed");
        refreshedCase.AuditLogs.Should().Contain(log => log.Action == "ComplaintPackGenerated");
    }

    private async Task<CaseDetailDto> CreateCaseAsync()
    {
        var response = await _client.PostAsJsonAsync(
            "/cases",
            new CreateCaseRequest(
                SeedData.DemoUserId,
                SeedData.ShopriteMerchantId,
                SeedData.ShopriteSandtonBranchId,
                "Main flow basket",
                DateTimeOffset.UtcNow.AddMinutes(-10),
                "ZAR",
                "FLOW-001",
                "Main API flow test."));

        var body = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(HttpStatusCode.Created, body);

        var createdCase = await response.Content.ReadFromJsonAsync<CaseDetailDto>();
        createdCase.Should().NotBeNull();
        return createdCase!;
    }

    private async Task<UploadedFileDto> UploadAsync(string fileName, string contentType, byte[] bytes, string category)
    {
        using var content = new MultipartFormDataContent();
        using var fileContent = new ByteArrayContent(bytes);
        fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse(contentType);
        content.Add(fileContent, "file", fileName);
        content.Add(new StringContent(category), "category");

        var response = await _client.PostAsync("/uploads", content);
        var body = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(HttpStatusCode.Created, body);

        var result = await response.Content.ReadFromJsonAsync<UploadedFileDto>();
        result.Should().NotBeNull();
        return result!;
    }
}
