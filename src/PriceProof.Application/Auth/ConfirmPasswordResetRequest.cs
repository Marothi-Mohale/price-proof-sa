using FluentValidation;
using PriceProof.Application.Common;

namespace PriceProof.Application.Auth;

public sealed record ConfirmPasswordResetRequest(string Email, string Token, string NewPassword);

public sealed class ConfirmPasswordResetRequestValidator : AbstractValidator<ConfirmPasswordResetRequest>
{
    public ConfirmPasswordResetRequestValidator()
    {
        RuleFor(request => request.Email)
            .NotEmpty()
            .EmailAddress()
            .MaximumLength(320);

        RuleFor(request => request.Token)
            .NotEmpty()
            .MaximumLength(256);

        RuleFor(request => request.NewPassword)
            .NotEmpty()
            .MaximumLength(256)
            .Must(SecurityPasswordRules.IsStrongPassword)
            .WithMessage(SecurityPasswordRules.PasswordRequirementsMessage);
    }
}
