namespace PriceProof.Application.Abstractions.Security;

public interface IAccountTokenService
{
    string GenerateToken();

    string HashToken(string token);
}
