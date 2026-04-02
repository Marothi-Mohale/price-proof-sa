using System.Security.Cryptography;
using System.Text;
using PriceProof.Application.Abstractions.Security;

namespace PriceProof.Infrastructure.Auth;

internal sealed class Pbkdf2PasswordHashingService : IPasswordHashingService
{
    private const int SaltSizeBytes = 16;
    private const int HashSizeBytes = 32;
    private const int DefaultIterations = 120_000;

    public PasswordHashResult HashPassword(string password)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(password);

        var salt = RandomNumberGenerator.GetBytes(SaltSizeBytes);
        var hash = Rfc2898DeriveBytes.Pbkdf2(
            Encoding.UTF8.GetBytes(password),
            salt,
            DefaultIterations,
            HashAlgorithmName.SHA512,
            HashSizeBytes);

        return new PasswordHashResult(
            Convert.ToBase64String(hash),
            Convert.ToBase64String(salt),
            DefaultIterations);
    }

    public bool VerifyPassword(string password, string passwordHash, string passwordSalt, int passwordIterations)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(password);

        if (string.IsNullOrWhiteSpace(passwordHash) || string.IsNullOrWhiteSpace(passwordSalt) || passwordIterations <= 0)
        {
            return false;
        }

        byte[] expectedHash;
        byte[] salt;

        try
        {
            expectedHash = Convert.FromBase64String(passwordHash);
            salt = Convert.FromBase64String(passwordSalt);
        }
        catch (FormatException)
        {
            return false;
        }

        var actualHash = Rfc2898DeriveBytes.Pbkdf2(
            Encoding.UTF8.GetBytes(password),
            salt,
            passwordIterations,
            HashAlgorithmName.SHA512,
            expectedHash.Length);

        return CryptographicOperations.FixedTimeEquals(expectedHash, actualHash);
    }
}
