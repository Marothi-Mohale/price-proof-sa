using FluentValidation;

namespace PriceProof.Application.Auth;

public sealed record AccountRecoveryRequest(string Email);

public sealed class AccountRecoveryRequestValidator : AbstractValidator<AccountRecoveryRequest>
{
    public AccountRecoveryRequestValidator()
    {
        RuleFor(request => request.Email)
            .NotEmpty()
            .EmailAddress()
            .MaximumLength(320);
    }
}
