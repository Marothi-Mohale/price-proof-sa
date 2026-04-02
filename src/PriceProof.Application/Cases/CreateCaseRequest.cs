using FluentValidation;

namespace PriceProof.Application.Cases;

public sealed record CreateCaseRequest(
    Guid ReportedByUserId,
    Guid? MerchantId,
    Guid? BranchId,
    string BasketDescription,
    DateTimeOffset IncidentAtUtc,
    string CurrencyCode,
    string? CustomerReference,
    string? Notes,
    string? CustomMerchantName = null);

public sealed class CreateCaseRequestValidator : AbstractValidator<CreateCaseRequest>
{
    public CreateCaseRequestValidator()
    {
        RuleFor(request => request.ReportedByUserId).NotEmpty();
        RuleFor(request => request.BasketDescription).NotEmpty().MaximumLength(500);
        RuleFor(request => request.CurrencyCode).NotEmpty().Length(3);
        RuleFor(request => request.IncidentAtUtc).LessThanOrEqualTo(DateTimeOffset.UtcNow.AddMinutes(5));
        RuleFor(request => request.CustomerReference).MaximumLength(64);
        RuleFor(request => request.Notes).MaximumLength(2000);
        RuleFor(request => request.CustomMerchantName)
            .Must(name => string.IsNullOrWhiteSpace(name) || name.Trim().Length >= 2)
            .WithMessage("Enter a merchant name with at least 2 characters.")
            .MaximumLength(200);
        RuleFor(request => request.MerchantId)
            .Must(merchantId => !merchantId.HasValue || merchantId.Value != Guid.Empty)
            .WithMessage("Choose a merchant.");
        RuleFor(request => request.BranchId)
            .Must(branchId => !branchId.HasValue || branchId.Value != Guid.Empty)
            .WithMessage("Choose a branch.");

        RuleFor(request => request).Custom((request, context) =>
        {
            var hasKnownMerchant = request.MerchantId.HasValue && request.MerchantId.Value != Guid.Empty;
            var hasCustomMerchant = !string.IsNullOrWhiteSpace(request.CustomMerchantName);

            if (!hasKnownMerchant && !hasCustomMerchant)
            {
                context.AddFailure(nameof(CreateCaseRequest.MerchantId), "Choose a merchant or enter a custom merchant.");
            }

            if (hasKnownMerchant && hasCustomMerchant)
            {
                context.AddFailure(nameof(CreateCaseRequest.CustomMerchantName), "Choose a merchant or enter a custom merchant, not both.");
            }

            if (!hasKnownMerchant && request.BranchId.HasValue)
            {
                context.AddFailure(nameof(CreateCaseRequest.BranchId), "A branch can only be selected for a known merchant.");
            }
        });
    }
}
