namespace PriceProof.Application.Auth;

public sealed record SessionTokenPayload(
    Guid UserId,
    string Email,
    bool IsAdmin,
    DateTimeOffset IssuedAtUtc);
