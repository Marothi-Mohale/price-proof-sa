namespace PriceProofSA.Api.Contracts;

public sealed record SignUpApiRequest(
    string Email,
    string DisplayName);

public sealed record SignInApiRequest(
    string Email);
