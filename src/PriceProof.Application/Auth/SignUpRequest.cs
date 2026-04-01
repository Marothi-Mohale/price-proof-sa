using FluentValidation;

namespace PriceProof.Application.Auth;

public sealed record SignUpRequest(
    string Email,
    string DisplayName);

public sealed class SignUpRequestValidator : AbstractValidator<SignUpRequest>
{
    public SignUpRequestValidator()
    {
        RuleFor(request => request.Email)
            .NotEmpty()
            .EmailAddress()
            .MaximumLength(320);

        RuleFor(request => request.DisplayName)
            .NotEmpty()
            .MaximumLength(120);
    }
}
