using PriceProof.Application.Auth;

namespace PriceProof.Application.Abstractions.Services;

public interface IAuthService
{
    Task<AuthSessionDto> SignUpAsync(SignUpRequest request, CancellationToken cancellationToken);

    Task<AuthSessionDto> SignInAsync(SignInRequest request, CancellationToken cancellationToken);

    Task<AuthActionResultDto> RequestEmailVerificationAsync(RequestEmailVerificationRequest request, CancellationToken cancellationToken);

    Task<AuthSessionDto> ConfirmEmailVerificationAsync(ConfirmEmailVerificationRequest request, CancellationToken cancellationToken);

    Task<AuthActionResultDto> RequestPasswordResetAsync(RequestPasswordResetRequest request, CancellationToken cancellationToken);

    Task<AuthSessionDto> ConfirmPasswordResetAsync(ConfirmPasswordResetRequest request, CancellationToken cancellationToken);

    Task<AuthActionResultDto> RecoverAccountAsync(AccountRecoveryRequest request, CancellationToken cancellationToken);

    Task<CurrentUserDto> GetCurrentUserAsync(CancellationToken cancellationToken);
}
