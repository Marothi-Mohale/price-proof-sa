using PriceProof.Application.Auth;

namespace PriceProof.Application.Abstractions.Services;

public interface IAdminAccessService
{
    Task<CurrentUserDto> RequireAdminAsync(string? authorizationHeader, CancellationToken cancellationToken);
}
