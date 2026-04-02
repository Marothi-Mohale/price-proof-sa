using Microsoft.EntityFrameworkCore;
using PriceProof.Application.Abstractions.Diagnostics;
using PriceProof.Application.Abstractions.Persistence;
using PriceProof.Application.Abstractions.Security;
using PriceProof.Application.Abstractions.Services;
using PriceProof.Application.Common;
using PriceProof.Application.Common.Exceptions;
using PriceProof.Domain.Entities;

namespace PriceProof.Application.Auth;

internal sealed class AuthService : IAuthService
{
    private readonly IAuditLogWriter _auditLogWriter;
    private readonly IApplicationDbContext _dbContext;
    private readonly ISessionTokenService _sessionTokenService;

    public AuthService(
        IApplicationDbContext dbContext,
        ISessionTokenService sessionTokenService,
        IAuditLogWriter auditLogWriter)
    {
        _dbContext = dbContext;
        _sessionTokenService = sessionTokenService;
        _auditLogWriter = auditLogWriter;
    }

    public async Task<AuthSessionDto> SignUpAsync(SignUpRequest request, CancellationToken cancellationToken)
    {
        request = request with
        {
            Email = InputSanitizer.SanitizeRequiredSingleLine(request.Email, 320),
            DisplayName = InputSanitizer.SanitizeRequiredSingleLine(request.DisplayName, 120)
        };

        var normalizedEmail = request.Email.Trim().ToUpperInvariant();
        var existingUser = await _dbContext.Users
            .SingleOrDefaultAsync(entity => entity.NormalizedEmail == normalizedEmail, cancellationToken);

        if (existingUser is not null)
        {
            if (!existingUser.IsActive)
            {
                var reactivatedAtUtc = DateTimeOffset.UtcNow;
                existingUser.Reactivate(reactivatedAtUtc);
                _auditLogWriter.Write(nameof(User), "UserReactivated", new { existingUser.Id, existingUser.Email }, reactivatedAtUtc, existingUser.Id);
                await _dbContext.SaveChangesAsync(cancellationToken);
            }

            await _auditLogWriter.WriteAndSaveAsync(
                nameof(User),
                "UserSignUpReturnedExistingAccount",
                new { existingUser.Id, existingUser.Email },
                DateTimeOffset.UtcNow,
                existingUser.Id,
                cancellationToken: cancellationToken);

            return MapSession(existingUser);
        }

        var user = User.Create(request.DisplayName, request.Email);
        _dbContext.Users.Add(user);
        _auditLogWriter.Write(nameof(User), "UserSignedUp", new { user.Id, user.Email }, DateTimeOffset.UtcNow, user.Id);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return MapSession(user);
    }

    public async Task<AuthSessionDto> SignInAsync(SignInRequest request, CancellationToken cancellationToken)
    {
        request = request with
        {
            Email = InputSanitizer.SanitizeRequiredSingleLine(request.Email, 320)
        };

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

        await _auditLogWriter.WriteAndSaveAsync(
            nameof(User),
            "UserSignedIn",
            new { user.Id, user.Email },
            DateTimeOffset.UtcNow,
            user.Id,
            cancellationToken: cancellationToken);

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

        return new CurrentUserDto(user.Id, user.Email, user.DisplayName, user.IsActive, user.IsAdmin);
    }

    private AuthSessionDto MapSession(User user)
    {
        return new AuthSessionDto(
            user.Id,
            user.Email,
            user.DisplayName,
            user.IsActive,
            user.IsAdmin,
            _sessionTokenService.CreateToken(
                new SessionTokenPayload(user.Id, user.Email, user.IsAdmin, DateTimeOffset.UtcNow),
                DateTimeOffset.UtcNow.AddHours(12)),
            DateTimeOffset.UtcNow);
    }
}
