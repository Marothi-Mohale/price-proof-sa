using PriceProofSA.Domain.Enums;

namespace PriceProofSA.Application.Cases;

public sealed record CreateCaseRequest(
    string MerchantName,
    string? MerchantCategory,
    string? BranchName,
    string? BranchAddress,
    string? BranchCity,
    string? BranchProvince,
    string BasketDescription);

public sealed record AddManualPriceCaptureRequest(
    decimal Amount,
    string? QuoteText,
    string? Notes);

public sealed record AddMediaPriceCaptureRequest(
    PriceCaptureMode Mode,
    decimal? Amount,
    string? QuoteText,
    string? Notes,
    string FileName,
    string ContentType,
    Stream Content);

public sealed record AddManualPaymentRequest(
    decimal Amount,
    PaymentInputMode Mode,
    bool IsCardPayment,
    string? Note,
    string? BankNotificationText);

public sealed record AddReceiptPaymentRequest(
    bool IsCardPayment,
    string? Note,
    decimal? EnteredAmount,
    string FileName,
    string ContentType,
    Stream Content);

public sealed record QrQuoteLockStubResponse(
    bool Available,
    string Message);

public sealed record CaseListItemDto(
    Guid Id,
    string MerchantName,
    string BasketDescription,
    string Status,
    decimal? QuotedAmount,
    decimal? ChargedAmount,
    decimal? DifferenceAmount,
    string Classification,
    bool LikelyUnlawfulCardSurcharge,
    DateTimeOffset UpdatedAtUtc);

public sealed record BranchDto(
    Guid Id,
    string Name,
    string? AddressLine,
    string? City,
    string? Province);

public sealed record PriceEvidenceDto(
    Guid Id,
    string FileType,
    string FileName,
    string ContentType,
    string StoragePath,
    DateTimeOffset UploadedAtUtc);

public sealed record PriceCaptureDto(
    Guid Id,
    string Mode,
    decimal? CapturedAmount,
    string? QuoteText,
    string? Notes,
    DateTimeOffset CapturedAtUtc,
    IReadOnlyCollection<PriceEvidenceDto> Evidence);

public sealed record ReceiptRecordDto(
    Guid Id,
    string FileName,
    string OcrStatus,
    decimal? ParsedTotalAmount,
    DateTimeOffset? ProcessedAtUtc,
    int RetryCount);

public sealed record PaymentRecordDto(
    Guid Id,
    string Mode,
    decimal? Amount,
    bool IsCardPayment,
    string? Note,
    string? RedactedBankNotificationText,
    DateTimeOffset CapturedAtUtc,
    ReceiptRecordDto? Receipt);

public sealed record ComplaintPackDto(
    Guid Id,
    string FileName,
    string StoragePath,
    string Summary,
    DateTimeOffset GeneratedAtUtc);

public sealed record MerchantRiskDto(
    int TotalReports,
    int ConfirmedSurchargeSignals,
    decimal Score,
    string Trend,
    DateTimeOffset LastCalculatedAtUtc);

public sealed record CaseDetailDto(
    Guid Id,
    Guid MerchantId,
    string MerchantName,
    BranchDto? Branch,
    string BasketDescription,
    string Status,
    decimal? QuotedAmount,
    decimal? ChargedAmount,
    decimal? DifferenceAmount,
    string Classification,
    bool LikelyUnlawfulCardSurcharge,
    string? ComplaintSummary,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc,
    IReadOnlyCollection<PriceCaptureDto> PriceCaptures,
    IReadOnlyCollection<PaymentRecordDto> PaymentRecords,
    ComplaintPackDto? LatestComplaintPack,
    MerchantRiskDto? MerchantRisk);
