using PriceProof.Application.Auth;

namespace PriceProof.Application.Abstractions.Services;

public interface IAuthService
{
    Task<AuthSessionDto> SignUpAsync(SignUpRequest request, CancellationToken cancellationToken);

    Task<AuthSessionDto> SignInAsync(SignInRequest request, CancellationToken cancellationToken);

    Task<CurrentUserDto> GetCurrentUserAsync(Guid userId, CancellationToken cancellationToken);
}
