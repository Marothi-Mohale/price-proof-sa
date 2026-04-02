using FluentValidation;

namespace PriceProof.Application.Auth;

public sealed record ConfirmEmailVerificationRequest(string Email, string Token);

public sealed class ConfirmEmailVerificationRequestValidator : AbstractValidator<ConfirmEmailVerificationRequest>
{
    public ConfirmEmailVerificationRequestValidator()
    {
        RuleFor(request => request.Email)
            .NotEmpty()
            .EmailAddress()
            .MaximumLength(320);

        RuleFor(request => request.Token)
            .NotEmpty()
            .MaximumLength(256);
    }
}
