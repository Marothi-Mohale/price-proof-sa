using FluentValidation;

namespace PriceProof.Application.Auth;

public sealed record SignInRequest(string Email);

public sealed class SignInRequestValidator : AbstractValidator<SignInRequest>
{
    public SignInRequestValidator()
    {
        RuleFor(request => request.Email)
            .NotEmpty()
            .EmailAddress()
            .MaximumLength(320);
    }
}
