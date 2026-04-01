using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using PriceProof.Application.Cases;
using PriceProof.Application.PaymentRecords;
using PriceProof.Application.PriceCaptures;
using PriceProof.Application.ReceiptRecords;
using PriceProof.Domain.Enums;
using PriceProof.Infrastructure.Seeding;

namespace PriceProof.IntegrationTests.Cases;

public sealed class EvidenceEndpointsTests : IClassFixture<PriceProofApiFactory>
{
    private readonly HttpClient _client;

    public EvidenceEndpointsTests(PriceProofApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Posting_evidence_should_progress_case_to_ready_for_review()
    {
        var createdCase = await CreateCaseAsync();

        var captureRequest = new CreatePriceCaptureRequest(
            createdCase.Id,
            SeedData.DemoUserId,
            CaptureType.PriceTagPhoto,
            EvidenceType.Image,
            129.99m,
            "ZAR",
            "price-tag.jpg",
            "/evidence/prices/price-tag.jpg",
            DateTimeOffset.UtcNow.AddMinutes(-12),
            "image/jpeg",
            "sha256:quoted-price",
            "Shelf label showed R129.99 before card payment.",
            "Photo captured at the aisle.");

        var captureResponse = await _client.PostAsJsonAsync("/price-captures", captureRequest);
        var captureBody = await captureResponse.Content.ReadAsStringAsync();
        captureResponse.StatusCode.Should().Be(HttpStatusCode.Created, captureBody);

        var capture = await captureResponse.Content.ReadFromJsonAsync<PriceCaptureDto>();
        capture.Should().NotBeNull();
        capture!.CaseClassification.Should().Be("PendingEvidence");

        var awaitingPaymentCase = await GetCaseAsync(createdCase.Id);
        awaitingPaymentCase.Status.Should().Be("AwaitingPayment");
        awaitingPaymentCase.PriceCaptures.Should().HaveCount(1);

        var paymentRequest = new CreatePaymentRecordRequest(
            createdCase.Id,
            SeedData.DemoUserId,
            PaymentMethod.CreditCard,
            139.99m,
            "ZAR",
            DateTimeOffset.UtcNow.AddMinutes(-8),
            "POS-20260401-0001",
            "SLIP-98421",
            "4242",
            "Customer paid by credit card at the till.");

        var paymentResponse = await _client.PostAsJsonAsync("/payment-records", paymentRequest);
        var paymentBody = await paymentResponse.Content.ReadAsStringAsync();
        paymentResponse.StatusCode.Should().Be(HttpStatusCode.Created, paymentBody);

        var payment = await paymentResponse.Content.ReadFromJsonAsync<PaymentRecordDto>();
        payment.Should().NotBeNull();
        payment!.CaseClassification.Should().Be("PotentialCardSurcharge");
        payment.DifferenceAmount.Should().Be(10.00m);

        var awaitingReceiptCase = await GetCaseAsync(createdCase.Id);
        awaitingReceiptCase.Status.Should().Be("AwaitingReceipt");
        awaitingReceiptCase.Classification.Should().Be("PotentialCardSurcharge");
        awaitingReceiptCase.DifferenceAmount.Should().Be(10.00m);

        var receiptRequest = new CreateReceiptRecordRequest(
            createdCase.Id,
            payment.Id,
            SeedData.DemoUserId,
            EvidenceType.Image,
            "receipt.jpg",
            "image/jpeg",
            "/evidence/receipts/receipt.jpg",
            DateTimeOffset.UtcNow.AddMinutes(-6),
            "ZAR",
            139.99m,
            "RCPT-20445",
            "Shoprite Sandton City",
            "TOTAL 139.99",
            "sha256:receipt");

        var receiptResponse = await _client.PostAsJsonAsync("/receipt-records", receiptRequest);
        var receiptBody = await receiptResponse.Content.ReadAsStringAsync();
        receiptResponse.StatusCode.Should().Be(HttpStatusCode.Created, receiptBody);

        var receipt = await receiptResponse.Content.ReadFromJsonAsync<ReceiptRecordDto>();
        receipt.Should().NotBeNull();
        receipt!.PaymentRecordId.Should().Be(payment.Id);
        receipt.ParsedTotalAmount.Should().Be(139.99m);

        var readyForReviewCase = await GetCaseAsync(createdCase.Id);
        readyForReviewCase.Status.Should().Be("ReadyForReview");
        readyForReviewCase.Classification.Should().Be("PotentialCardSurcharge");
        readyForReviewCase.PaymentRecords.Should().ContainSingle();
        readyForReviewCase.PaymentRecords.Single().Receipt.Should().NotBeNull();
        readyForReviewCase.PaymentRecords.Single().Receipt!.ReceiptNumber.Should().Be("RCPT-20445");
    }

    [Fact]
    public async Task Posting_a_second_receipt_for_the_same_payment_should_conflict()
    {
        var createdCase = await CreateCaseAsync();

        var paymentResponse = await _client.PostAsJsonAsync(
            "/payment-records",
            new CreatePaymentRecordRequest(
                createdCase.Id,
                SeedData.DemoUserId,
                PaymentMethod.Cash,
                89.99m,
                "ZAR",
                DateTimeOffset.UtcNow.AddMinutes(-4),
                "POS-20260401-0002",
                null,
                null,
                null));

        var paymentBody = await paymentResponse.Content.ReadAsStringAsync();
        paymentResponse.StatusCode.Should().Be(HttpStatusCode.Created, paymentBody);
        var payment = await paymentResponse.Content.ReadFromJsonAsync<PaymentRecordDto>();
        payment.Should().NotBeNull();

        var firstReceiptRequest = new CreateReceiptRecordRequest(
            createdCase.Id,
            payment!.Id,
            SeedData.DemoUserId,
            EvidenceType.Image,
            "receipt-1.jpg",
            "image/jpeg",
            "/evidence/receipts/receipt-1.jpg",
            DateTimeOffset.UtcNow.AddMinutes(-3),
            "ZAR",
            89.99m,
            "RCPT-1001",
            "Shoprite",
            null,
            null);

        var firstReceiptResponse = await _client.PostAsJsonAsync("/receipt-records", firstReceiptRequest);
        var firstReceiptBody = await firstReceiptResponse.Content.ReadAsStringAsync();
        firstReceiptResponse.StatusCode.Should().Be(HttpStatusCode.Created, firstReceiptBody);

        var secondReceiptRequest = firstReceiptRequest with
        {
            FileName = "receipt-2.jpg",
            StoragePath = "/evidence/receipts/receipt-2.jpg",
            ReceiptNumber = "RCPT-1002"
        };

        var secondReceiptResponse = await _client.PostAsJsonAsync("/receipt-records", secondReceiptRequest);
        var secondReceiptBody = await secondReceiptResponse.Content.ReadAsStringAsync();

        secondReceiptResponse.StatusCode.Should().Be(HttpStatusCode.Conflict, secondReceiptBody);
        secondReceiptBody.Should().Contain("already been attached");
    }

    private async Task<CaseDetailDto> CreateCaseAsync()
    {
        var request = new CreateCaseRequest(
            SeedData.DemoUserId,
            SeedData.ShopriteMerchantId,
            SeedData.ShopriteSandtonBranchId,
            "Basket of household staples disputed at checkout",
            DateTimeOffset.UtcNow.AddMinutes(-20),
            "ZAR",
            "CASE-INTEGRATION",
            "Evidence flow integration test.");

        var response = await _client.PostAsJsonAsync("/cases", request);
        var body = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(HttpStatusCode.Created, body);

        var createdCase = await response.Content.ReadFromJsonAsync<CaseDetailDto>();
        createdCase.Should().NotBeNull();
        return createdCase!;
    }

    private async Task<CaseDetailDto> GetCaseAsync(Guid caseId)
    {
        var response = await _client.GetAsync($"/cases/{caseId}");
        var body = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(HttpStatusCode.OK, body);

        var discrepancyCase = await response.Content.ReadFromJsonAsync<CaseDetailDto>();
        discrepancyCase.Should().NotBeNull();
        return discrepancyCase!;
    }
}
