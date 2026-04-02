using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using PriceProof.Application.Auth;
using PriceProof.Application.Cases;
using PriceProof.Application.ComplaintPacks;
using PriceProof.Application.PaymentRecords;
using PriceProof.Application.PriceCaptures;
using PriceProof.Application.ReceiptRecords;
using PriceProof.Domain.Enums;
using PriceProof.Infrastructure.Seeding;

namespace PriceProof.IntegrationTests.ComplaintPacks;

public sealed class ComplaintPackEndpointsTests : IClassFixture<PriceProofApiFactory>
{
    private static readonly byte[] TinyPngBytes = Convert.FromBase64String(
        "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAusB9WlH0n0AAAAASUVORK5CYII=");

    private readonly PriceProofApiFactory _factory;
    private readonly HttpClient _client;
    private readonly AuthSessionDto _session;

    public ComplaintPackEndpointsTests(PriceProofApiFactory factory)
    {
        _factory = factory;
        (_client, _session) = factory.CreateAuthenticatedClientAsync().GetAwaiter().GetResult();
    }

    [Fact]
    public async Task Generate_and_download_complaint_pack_should_return_pdf_and_structured_summary()
    {
        await CreateEvidenceFileAsync("evidence/prices/display.png");
        await CreateEvidenceFileAsync("evidence/receipts/receipt.png");

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
                "display.png",
                "evidence/prices/display.png",
                DateTimeOffset.UtcNow.AddMinutes(-9),
                "image/png",
                null,
                "Displayed shelf price was R199.99.",
                null));

        var paymentResponse = await _client.PostAsJsonAsync(
            "/payment-records",
            new CreatePaymentRecordRequest(
                createdCase.Id,
                _session.UserId,
                PaymentMethod.DebitCard,
                209.99m,
                "ZAR",
                DateTimeOffset.UtcNow.AddMinutes(-6),
                "POS-COMPLAINT-001",
                null,
                "1234",
                "Merchant said a card fee applied."));

        paymentResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var payment = await paymentResponse.Content.ReadFromJsonAsync<PaymentRecordDto>();
        payment.Should().NotBeNull();

        await _client.PostAsJsonAsync(
            "/receipt-records",
            new CreateReceiptRecordRequest(
                createdCase.Id,
                payment!.Id,
                _session.UserId,
                EvidenceType.Image,
                "receipt.png",
                "image/png",
                "evidence/receipts/receipt.png",
                DateTimeOffset.UtcNow.AddMinutes(-5),
                "ZAR",
                209.99m,
                "RCT-001",
                "Shoprite Sandton City",
                "TOTAL 209.99",
                null));

        await _client.PostAsJsonAsync(
            $"/cases/{createdCase.Id}/analyze",
            new AnalyzeCaseRequest(MerchantSaidCardFee: true));

        var generateResponse = await _client.PostAsync($"/cases/{createdCase.Id}/generate-complaint-pack", content: null);
        var generateBody = await generateResponse.Content.ReadAsStringAsync();
        generateResponse.StatusCode.Should().Be(HttpStatusCode.Created, generateBody);

        var generatedPack = await generateResponse.Content.ReadFromJsonAsync<GeneratedComplaintPackDto>();
        generatedPack.Should().NotBeNull();
        generatedPack!.CaseId.Should().Be(createdCase.Id);
        generatedPack.FileName.Should().EndWith(".pdf");
        generatedPack.ContentType.Should().Be("application/pdf");
        generatedPack.DownloadUrl.Should().Be($"/complaint-packs/{generatedPack.Id}/download");
        generatedPack.JsonSummary.Amounts.QuotedAmount.Should().Be(199.99m);
        generatedPack.JsonSummary.Amounts.ChargedAmount.Should().Be(209.99m);
        generatedPack.JsonSummary.Amounts.DiscrepancyAmount.Should().Be(10.00m);
        generatedPack.JsonSummary.Analysis.Classification.Should().Be("LikelyCardSurcharge");
        generatedPack.JsonSummary.Analysis.ClassificationLabel.Should().Be("Likely Card Surcharge");
        generatedPack.JsonSummary.EvidenceAssessment.Strength.Should().Be("Strong");
        generatedPack.JsonSummary.EvidenceInventory.Should().HaveCount(2);
        generatedPack.JsonSummary.SubmissionGuidance.RecommendedRoutes.Should().Contain(route => route.Channel.Contains("Merchant"));
        generatedPack.JsonSummary.SubmissionGuidance.RecommendedRoutes.Should().Contain(route => route.Channel.Contains("Bank dispute team"));
        generatedPack.JsonSummary.SubmissionGuidance.EmailTemplate.Subject.Should().Contain(createdCase.CaseNumber);
        generatedPack.JsonSummary.SubmissionGuidance.EmailTemplate.Body.Should().Contain("Please find attached a complaint pack");
        generatedPack.Summary.Should().Contain("case reference");

        var downloadResponse = await _client.GetAsync(generatedPack.DownloadUrl);
        downloadResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        downloadResponse.Content.Headers.ContentType?.MediaType.Should().Be("application/pdf");

        var pdfBytes = await downloadResponse.Content.ReadAsByteArrayAsync();
        pdfBytes.Should().NotBeEmpty();
        System.Text.Encoding.ASCII.GetString(pdfBytes.Take(4).ToArray()).Should().Be("%PDF");

        var refreshedCase = await _client.GetFromJsonAsync<CaseDetailDto>($"/cases/{createdCase.Id}");
        refreshedCase.Should().NotBeNull();
        refreshedCase!.ComplaintPacks.Should().ContainSingle();
        refreshedCase.ComplaintPacks.Single().DownloadUrl.Should().Be(generatedPack.DownloadUrl);
    }

    private async Task<CaseDetailDto> CreateCaseAsync()
    {
        var response = await _client.PostAsJsonAsync(
            "/cases",
            new CreateCaseRequest(
                _session.UserId,
                SeedData.ShopriteMerchantId,
                SeedData.ShopriteSandtonBranchId,
                "Complaint pack endpoint test basket",
                DateTimeOffset.UtcNow.AddMinutes(-15),
                "ZAR",
                "COMPLAINT-PACK-TEST",
                "Complaint pack integration test."));

        var body = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(HttpStatusCode.Created, body);

        var createdCase = await response.Content.ReadFromJsonAsync<CaseDetailDto>();
        createdCase.Should().NotBeNull();
        return createdCase!;
    }

    private async Task CreateEvidenceFileAsync(string relativePath)
    {
        var absolutePath = Path.Combine(
            _factory.StorageRootPath,
            relativePath.Replace('/', Path.DirectorySeparatorChar).Replace('\\', Path.DirectorySeparatorChar));

        var directory = Path.GetDirectoryName(absolutePath);
        directory.Should().NotBeNull();
        Directory.CreateDirectory(directory!);
        await File.WriteAllBytesAsync(absolutePath, TinyPngBytes);
    }
}
