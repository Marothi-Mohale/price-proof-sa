namespace PriceProof.Application.Abstractions.Security;

public interface IPasswordHashingService
{
    PasswordHashResult HashPassword(string password);

    bool VerifyPassword(string password, string passwordHash, string passwordSalt, int passwordIterations);
}

public sealed record PasswordHashResult(string PasswordHash, string PasswordSalt, int PasswordIterations);
