using FluentValidation;

namespace PriceProof.Application.Cases;

public sealed record CreateCaseRequest(
    Guid ReportedByUserId,
    Guid MerchantId,
    Guid? BranchId,
    string BasketDescription,
    DateTimeOffset IncidentAtUtc,
    string CurrencyCode,
    string? CustomerReference,
    string? Notes);

public sealed class CreateCaseRequestValidator : AbstractValidator<CreateCaseRequest>
{
    public CreateCaseRequestValidator()
    {
        RuleFor(request => request.ReportedByUserId).NotEmpty();
        RuleFor(request => request.MerchantId).NotEmpty();
        RuleFor(request => request.BasketDescription).NotEmpty().MaximumLength(500);
        RuleFor(request => request.CurrencyCode).NotEmpty().Length(3);
        RuleFor(request => request.IncidentAtUtc).LessThanOrEqualTo(DateTimeOffset.UtcNow.AddMinutes(5));
        RuleFor(request => request.CustomerReference).MaximumLength(64);
        RuleFor(request => request.Notes).MaximumLength(2000);
    }
}
