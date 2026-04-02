namespace PriceProof.Application.Abstractions.Security;

public interface IAccountWorkflowUrlBuilder
{
    string BuildEmailVerificationUrl(string email, string token);

    string BuildPasswordResetUrl(string email, string token);
}
