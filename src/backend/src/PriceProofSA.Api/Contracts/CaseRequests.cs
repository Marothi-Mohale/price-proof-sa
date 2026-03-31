using Microsoft.AspNetCore.Http;
using PriceProofSA.Domain.Enums;

namespace PriceProofSA.Api.Contracts;

public sealed record CreateCaseApiRequest(
    string MerchantName,
    string? MerchantCategory,
    string? BranchName,
    string? BranchAddress,
    string? BranchCity,
    string? BranchProvince,
    string BasketDescription);

public sealed record AddManualPriceCaptureApiRequest(
    decimal Amount,
    string? QuoteText,
    string? Notes);

public sealed class AddMediaPriceCaptureApiRequest
{
    public PriceCaptureMode Mode { get; set; }

    public decimal? Amount { get; set; }

    public string? QuoteText { get; set; }

    public string? Notes { get; set; }

    public IFormFile? File { get; set; }
}

public sealed record AddManualPaymentApiRequest(
    decimal Amount,
    PaymentInputMode Mode,
    bool IsCardPayment,
    string? Note,
    string? BankNotificationText);

public sealed class AddReceiptPaymentApiRequest
{
    public bool IsCardPayment { get; set; }

    public string? Note { get; set; }

    public decimal? EnteredAmount { get; set; }

    public IFormFile? File { get; set; }
}
