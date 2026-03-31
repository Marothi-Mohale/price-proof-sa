using FluentValidation;
using PriceProof.Domain.Enums;

namespace PriceProof.Application.PriceCaptures;

public sealed record CreatePriceCaptureRequest(
    Guid CaseId,
    Guid CapturedByUserId,
    CaptureType CaptureType,
    EvidenceType EvidenceType,
    decimal? QuotedAmount,
    string CurrencyCode,
    string FileName,
    string EvidenceStoragePath,
    DateTimeOffset CapturedAtUtc,
    string? ContentType,
    string? EvidenceHash,
    string? MerchantStatement,
    string? Notes);

public sealed class CreatePriceCaptureRequestValidator : AbstractValidator<CreatePriceCaptureRequest>
{
    public CreatePriceCaptureRequestValidator()
    {
        RuleFor(request => request.CaseId).NotEmpty();
        RuleFor(request => request.CapturedByUserId).NotEmpty();
        RuleFor(request => request.CaptureType).IsInEnum();
        RuleFor(request => request.EvidenceType).IsInEnum();
        RuleFor(request => request.QuotedAmount).GreaterThanOrEqualTo(0).When(request => request.QuotedAmount.HasValue);
        RuleFor(request => request.CurrencyCode).NotEmpty().Length(3);
        RuleFor(request => request.FileName).NotEmpty().MaximumLength(260);
        RuleFor(request => request.EvidenceStoragePath).NotEmpty().MaximumLength(500);
        RuleFor(request => request.ContentType).MaximumLength(120);
        RuleFor(request => request.EvidenceHash).MaximumLength(128);
        RuleFor(request => request.MerchantStatement).MaximumLength(2000);
        RuleFor(request => request.Notes).MaximumLength(2000);
    }
}
