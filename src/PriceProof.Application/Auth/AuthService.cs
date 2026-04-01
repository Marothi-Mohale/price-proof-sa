using Microsoft.EntityFrameworkCore;
using PriceProof.Application.Abstractions.Persistence;
using PriceProof.Application.Abstractions.Services;
using PriceProof.Application.Common.Exceptions;
using PriceProof.Domain.Entities;

namespace PriceProof.Application.Auth;

internal sealed class AuthService : IAuthService
{
    private readonly IApplicationDbContext _dbContext;

    public AuthService(IApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<AuthSessionDto> SignUpAsync(SignUpRequest request, CancellationToken cancellationToken)
    {
        var normalizedEmail = request.Email.Trim().ToUpperInvariant();
        var existingUser = await _dbContext.Users
            .SingleOrDefaultAsync(entity => entity.NormalizedEmail == normalizedEmail, cancellationToken);

        if (existingUser is not null)
        {
            if (!existingUser.IsActive)
            {
                existingUser.Reactivate(DateTimeOffset.UtcNow);
                await _dbContext.SaveChangesAsync(cancellationToken);
            }

            return MapSession(existingUser);
        }

        var user = User.Create(request.DisplayName, request.Email);
        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return MapSession(user);
    }

    public async Task<AuthSessionDto> SignInAsync(SignInRequest request, CancellationToken cancellationToken)
    {
        var normalizedEmail = request.Email.Trim().ToUpperInvariant();
        var user = await _dbContext.Users
            .SingleOrDefaultAsync(entity => entity.NormalizedEmail == normalizedEmail, cancellationToken);

        if (user is null)
        {
            throw new NotFoundException("No user exists for the supplied email address.");
        }

        if (!user.IsActive)
        {
            throw new ConflictException("This account is currently inactive.");
        }

        return MapSession(user);
    }

    public async Task<CurrentUserDto> GetCurrentUserAsync(Guid userId, CancellationToken cancellationToken)
    {
        var user = await _dbContext.Users
            .SingleOrDefaultAsync(entity => entity.Id == userId, cancellationToken);

        if (user is null)
        {
            throw new NotFoundException($"User '{userId}' was not found.");
        }

        return new CurrentUserDto(user.Id, user.Email, user.DisplayName, user.IsActive);
    }

    private static AuthSessionDto MapSession(User user)
    {
        return new AuthSessionDto(
            user.Id,
            user.Email,
            user.DisplayName,
            user.IsActive,
            DateTimeOffset.UtcNow);
    }
}
