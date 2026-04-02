using FluentValidation;

namespace PriceProof.Application.Auth;

public sealed record RequestEmailVerificationRequest(string Email);

public sealed class RequestEmailVerificationRequestValidator : AbstractValidator<RequestEmailVerificationRequest>
{
    public RequestEmailVerificationRequestValidator()
    {
        RuleFor(request => request.Email)
            .NotEmpty()
            .EmailAddress()
            .MaximumLength(320);
    }
}
