using FluentValidation;
using PriceProof.Application.Common;

namespace PriceProof.Application.Auth;

public sealed record SignUpRequest(
    string Email,
    string DisplayName,
    string Password);

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

        RuleFor(request => request.Password)
            .NotEmpty()
            .MaximumLength(256)
            .Must(SecurityPasswordRules.IsStrongPassword)
            .WithMessage(SecurityPasswordRules.PasswordRequirementsMessage);
    }
}
