using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.WebUtilities;
using PriceProof.Application.Auth;

namespace PriceProof.IntegrationTests.Auth;

public sealed class AuthEndpointsTests : IClassFixture<PriceProofApiFactory>
{
    private readonly PriceProofApiFactory _factory;

    public AuthEndpointsTests(PriceProofApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task SignIn_ShouldReject_UnverifiedAccount()
    {
        var client = _factory.CreateClient(new()
        {
            AllowAutoRedirect = false,
            HandleCookies = true
        });
        client.DefaultRequestHeaders.UserAgent.ParseAdd($"priceproof-tests/{Guid.NewGuid():N}");

        var unique = Guid.NewGuid().ToString("N");
        var email = $"pending-{unique}@priceproof.test";
        var signUpResponse = await client.PostAsJsonAsync(
            "/auth/sign-up",
            new SignUpRequest(email, $"Pending {unique[..6]}", TestAuth.DefaultPassword));

        signUpResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var signInResponse = await client.PostAsJsonAsync("/auth/sign-in", new SignInRequest(email, TestAuth.DefaultPassword));
        signInResponse.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await signInResponse.Content.ReadAsStringAsync()).Should().Contain("Verify your email");
    }

    [Fact]
    public async Task PasswordReset_ShouldIssueResetLink_AndCreateFreshSession()
    {
        var client = _factory.CreateClient(new()
        {
            AllowAutoRedirect = false,
            HandleCookies = true
        });
        client.DefaultRequestHeaders.UserAgent.ParseAdd($"priceproof-tests/{Guid.NewGuid():N}");

        var session = await TestAuth.SignUpAsync(_factory, client);
        var resetRequestResponse = await client.PostAsJsonAsync(
            "/auth/password-reset/request",
            new RequestPasswordResetRequest(session.Email));

        resetRequestResponse.StatusCode.Should().Be(HttpStatusCode.Accepted);

        var resetUrl = _factory.EmailDeliveryService.GetLatestUrl(session.Email, "/auth/reset-password");
        var resetUri = new Uri(resetUrl);
        var query = QueryHelpers.ParseQuery(resetUri.Query);

        var confirmResponse = await client.PostAsJsonAsync(
            "/auth/password-reset/confirm",
            new ConfirmPasswordResetRequest(query["email"].ToString(), query["token"].ToString(), "Password!Updated123"));

        confirmResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var nextSession = await confirmResponse.Content.ReadFromJsonAsync<AuthSessionDto>();
        nextSession.Should().NotBeNull();
        nextSession!.SignedInAtUtc.Should().NotBeNull();

        await client.PostAsJsonAsync("/auth/sign-out", new { });
        var signInResponse = await client.PostAsJsonAsync("/auth/sign-in", new SignInRequest(session.Email, "Password!Updated123"));
        signInResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task RepeatedFailedSignIns_ShouldLockAccount_UntilRecoveryCompletes()
    {
        var client = _factory.CreateClient(new()
        {
            AllowAutoRedirect = false,
            HandleCookies = true
        });
        client.DefaultRequestHeaders.UserAgent.ParseAdd($"priceproof-tests/{Guid.NewGuid():N}");

        var session = await TestAuth.SignUpAsync(_factory, client);

        for (var attempt = 0; attempt < 3; attempt++)
        {
            var failedResponse = await client.PostAsJsonAsync("/auth/sign-in", new SignInRequest(session.Email, "Wrong!Password123"));
            failedResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }

        var lockedResponse = await client.PostAsJsonAsync("/auth/sign-in", new SignInRequest(session.Email, TestAuth.DefaultPassword));
        lockedResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await lockedResponse.Content.ReadAsStringAsync()).Should().Contain("temporarily locked");

        var recoveryResponse = await client.PostAsJsonAsync("/auth/account-recovery", new AccountRecoveryRequest(session.Email));
        recoveryResponse.StatusCode.Should().Be(HttpStatusCode.Accepted);

        var resetUrl = _factory.EmailDeliveryService.GetLatestUrl(session.Email, "/auth/reset-password");
        var resetUri = new Uri(resetUrl);
        var query = QueryHelpers.ParseQuery(resetUri.Query);

        var confirmResponse = await client.PostAsJsonAsync(
            "/auth/password-reset/confirm",
            new ConfirmPasswordResetRequest(query["email"].ToString(), query["token"].ToString(), "Recovered!Password123"));

        confirmResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        await client.PostAsJsonAsync("/auth/sign-out", new { });
        var recoveredSignIn = await client.PostAsJsonAsync("/auth/sign-in", new SignInRequest(session.Email, "Recovered!Password123"));
        recoveredSignIn.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
