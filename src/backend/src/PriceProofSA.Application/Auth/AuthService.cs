using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PriceProofSA.Application.Abstractions.Persistence;
using PriceProofSA.Application.Abstractions.Time;
using PriceProofSA.Application.Common.Exceptions;
using PriceProofSA.Domain.Entities;
using PriceProofSA.Domain.Enums;

namespace PriceProofSA.Application.Auth;

public sealed class AuthService
{
    private static readonly TimeSpan SessionLifetime = TimeSpan.FromDays(14);

    private readonly IApplicationDbContext _dbContext;
    private readonly IClock _clock;
    private readonly ILogger<AuthService> _logger;

    public AuthService(IApplicationDbContext dbContext, IClock clock, ILogger<AuthService> logger)
    {
        _dbContext = dbContext;
        _clock = clock;
        _logger = logger;
    }

    public async Task<AuthSessionResponse> SignUpAsync(SignUpRequest request, IReadOnlyCollection<string> adminEmails, CancellationToken cancellationToken = default)
    {
        ValidateSignUp(request);

        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        var user = await _dbContext.Users
            .Include(static item => item.Sessions)
            .SingleOrDefaultAsync(item => item.NormalizedEmail == normalizedEmail, cancellationToken);

        if (user is null)
        {
            var role = adminEmails.Contains(normalizedEmail, StringComparer.OrdinalIgnoreCase)
                ? UserRole.Admin
                : UserRole.User;

            user = AppUser.Create(request.Email, request.DisplayName, role, _clock.UtcNow);
            await _dbContext.Users.AddAsync(user, cancellationToken);
        }
        else
        {
            user.Touch(_clock.UtcNow);
        }

        var response = CreateSession(user);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Issued sign-up session for {Email}", request.Email);
        return response;
    }

    public async Task<AuthSessionResponse> SignInAsync(SignInRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Email))
        {
            throw new InputValidationException(
                "Sign-in failed validation.",
                new Dictionary<string, string[]>
                {
                    ["email"] = ["Email is required."]
                });
        }

        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        var user = await _dbContext.Users
            .Include(static item => item.Sessions)
            .SingleOrDefaultAsync(item => item.NormalizedEmail == normalizedEmail, cancellationToken)
            ?? throw new AppNotFoundException("No user exists for the supplied email.");

        user.Touch(_clock.UtcNow);
        var response = CreateSession(user);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Issued sign-in session for {Email}", request.Email);
        return response;
    }

    public async Task<CurrentUserDto?> GetCurrentUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Users
            .Where(user => user.Id == userId)
            .Select(user => new CurrentUserDto(user.Id, user.Email, user.DisplayName, user.Role.ToString()))
            .SingleOrDefaultAsync(cancellationToken);
    }

    private AuthSessionResponse CreateSession(AppUser user)
    {
        var rawToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
            .Replace('+', '-')
            .Replace('/', '_')
            .TrimEnd('=');

        var session = UserSession.Create(user.Id, SessionTokenHasher.Hash(rawToken), _clock.UtcNow, SessionLifetime);
        user.AddSession(session);
        _dbContext.UserSessions.Add(session);

        return new AuthSessionResponse(
            user.Id,
            user.Email,
            user.DisplayName,
            user.Role.ToString(),
            rawToken,
            session.ExpiresAtUtc);
    }

    private static void ValidateSignUp(SignUpRequest request)
    {
        var errors = new Dictionary<string, string[]>();

        if (string.IsNullOrWhiteSpace(request.Email) || !request.Email.Contains('@', StringComparison.Ordinal))
        {
            errors["email"] = ["A valid email is required."];
        }

        if (string.IsNullOrWhiteSpace(request.DisplayName))
        {
            errors["displayName"] = ["Display name is required."];
        }

        if (errors.Count > 0)
        {
            throw new InputValidationException("Sign-up failed validation.", errors);
        }
    }
}
