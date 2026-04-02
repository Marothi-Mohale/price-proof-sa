using FluentValidation;
using PriceProof.Application.Common;

namespace PriceProof.Application.Auth;

public sealed record SignInRequest(string Email, string Password);

public sealed class SignInRequestValidator : AbstractValidator<SignInRequest>
{
    public SignInRequestValidator()
    {
        RuleFor(request => request.Email)
            .NotEmpty()
            .EmailAddress()
            .MaximumLength(320);

        RuleFor(request => request.Password)
            .NotEmpty()
            .MaximumLength(256)
            .Must(SecurityPasswordRules.IsStrongPassword)
            .WithMessage(SecurityPasswordRules.PasswordRequirementsMessage);
    }
}
