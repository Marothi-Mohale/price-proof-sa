using System.Security.Cryptography;
using System.Text;
using PriceProof.Application.Abstractions.Security;

namespace PriceProof.Infrastructure.Auth;

internal sealed class AccountTokenService : IAccountTokenService
{
    public string GenerateToken()
    {
        Span<byte> bytes = stackalloc byte[32];
        RandomNumberGenerator.Fill(bytes);
        return Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }

    public string HashToken(string token)
    {
        var normalized = token.Trim();
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(normalized))).ToLowerInvariant();
    }
}
