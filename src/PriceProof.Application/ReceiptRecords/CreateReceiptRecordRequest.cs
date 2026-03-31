using FluentValidation;
using PriceProof.Domain.Enums;

namespace PriceProof.Application.ReceiptRecords;

public sealed record CreateReceiptRecordRequest(
    Guid CaseId,
    Guid PaymentRecordId,
    Guid UploadedByUserId,
    EvidenceType EvidenceType,
    string FileName,
    string ContentType,
    string StoragePath,
    DateTimeOffset UploadedAtUtc,
    string CurrencyCode,
    decimal? ParsedTotalAmount,
    string? ReceiptNumber,
    string? MerchantName,
    string? RawText,
    string? FileHash);

public sealed class CreateReceiptRecordRequestValidator : AbstractValidator<CreateReceiptRecordRequest>
{
    public CreateReceiptRecordRequestValidator()
    {
        RuleFor(request => request.CaseId).NotEmpty();
        RuleFor(request => request.PaymentRecordId).NotEmpty();
        RuleFor(request => request.UploadedByUserId).NotEmpty();
        RuleFor(request => request.EvidenceType).IsInEnum();
        RuleFor(request => request.FileName).NotEmpty().MaximumLength(260);
        RuleFor(request => request.ContentType).NotEmpty().MaximumLength(120);
        RuleFor(request => request.StoragePath).NotEmpty().MaximumLength(500);
        RuleFor(request => request.CurrencyCode).NotEmpty().Length(3);
        RuleFor(request => request.ParsedTotalAmount).GreaterThan(0).When(request => request.ParsedTotalAmount.HasValue);
        RuleFor(request => request.ReceiptNumber).MaximumLength(64);
        RuleFor(request => request.MerchantName).MaximumLength(200);
        RuleFor(request => request.RawText).MaximumLength(16000);
        RuleFor(request => request.FileHash).MaximumLength(128);
    }
}
