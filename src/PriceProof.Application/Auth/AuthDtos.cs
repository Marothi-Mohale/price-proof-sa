namespace PriceProof.Application.Auth;

public sealed record AuthSessionDto(
    Guid UserId,
    string Email,
    string DisplayName,
    bool IsActive,
    bool IsAdmin,
    DateTimeOffset SignedInAtUtc);

public sealed record CurrentUserDto(
    Guid Id,
    string Email,
    string DisplayName,
    bool IsActive,
    bool IsAdmin);
