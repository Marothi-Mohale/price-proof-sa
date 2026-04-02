using PriceProof.Application.Auth;

namespace PriceProof.Application.Abstractions.Security;

public interface ISessionTokenService
{
    string CreateToken(SessionTokenPayload payload, DateTimeOffset expiresAtUtc);

    bool TryReadToken(string token, out SessionTokenPayload? payload);
}
