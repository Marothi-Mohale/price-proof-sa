namespace PriceProofSA.Application.Auth;

public sealed record SignUpRequest(
    string Email,
    string DisplayName);

public sealed record SignInRequest(
    string Email);

public sealed record AuthSessionResponse(
    Guid UserId,
    string Email,
    string DisplayName,
    string Role,
    string SessionToken,
    DateTimeOffset ExpiresAtUtc);

public sealed record CurrentUserDto(
    Guid Id,
    string Email,
    string DisplayName,
    string Role);
