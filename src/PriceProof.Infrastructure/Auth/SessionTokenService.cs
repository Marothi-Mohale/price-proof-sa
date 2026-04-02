using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.DataProtection;
using PriceProof.Application.Abstractions.Security;
using PriceProof.Application.Auth;

namespace PriceProof.Infrastructure.Auth;

internal sealed class SessionTokenService : ISessionTokenService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly ITimeLimitedDataProtector _protector;

    public SessionTokenService(IDataProtectionProvider dataProtectionProvider)
    {
        _protector = dataProtectionProvider
            .CreateProtector("PriceProof.SessionToken")
            .ToTimeLimitedDataProtector();
    }

    public string CreateToken(SessionTokenPayload payload, DateTimeOffset expiresAtUtc)
    {
        var serialized = JsonSerializer.Serialize(payload, JsonOptions);
        var protectedBytes = _protector.Protect(Encoding.UTF8.GetBytes(serialized), expiresAtUtc);
        return Convert.ToBase64String(protectedBytes);
    }

    public bool TryReadToken(string token, out SessionTokenPayload? payload)
    {
        payload = null;

        if (string.IsNullOrWhiteSpace(token))
        {
            return false;
        }

        try
        {
            var protectedBytes = Convert.FromBase64String(token);
            var rawBytes = _protector.Unprotect(protectedBytes, out _);
            payload = JsonSerializer.Deserialize<SessionTokenPayload>(rawBytes, JsonOptions);
            return payload is not null;
        }
        catch
        {
            return false;
        }
    }
}
