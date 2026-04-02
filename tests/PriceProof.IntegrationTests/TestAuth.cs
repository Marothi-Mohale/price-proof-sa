using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.WebUtilities;
using PriceProof.Application.Auth;

namespace PriceProof.IntegrationTests;

internal static class TestAuth
{
    public const string DefaultPassword = "User!SecurePass123";

    public static async Task<(HttpClient Client, AuthSessionDto Session)> CreateAuthenticatedClientAsync(
        this PriceProofApiFactory factory,
        bool admin = false)
    {
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = true
        });
        client.DefaultRequestHeaders.UserAgent.ParseAdd($"priceproof-tests/{Guid.NewGuid():N}");

        var session = admin
            ? await SignInAsync(client, PriceProofApiFactory.BootstrapAdminEmail, PriceProofApiFactory.BootstrapAdminPassword)
            : await SignUpAsync(factory, client);

        return (client, session);
    }

    public static async Task<AuthSessionDto> SignUpAsync(PriceProofApiFactory factory, HttpClient client, string? email = null, string? displayName = null)
    {
        var unique = Guid.NewGuid().ToString("N");
        var selectedEmail = email ?? $"user-{unique}@priceproof.test";
        var response = await client.PostAsJsonAsync(
            "/auth/sign-up",
            new SignUpRequest(
                selectedEmail,
                displayName ?? $"Test User {unique[..6]}",
                DefaultPassword));

        var body = await response.Content.ReadAsStringAsync();
        response.IsSuccessStatusCode.Should().BeTrue(body);

        var pending = await response.Content.ReadFromJsonAsync<AuthSessionDto>();
        pending.Should().NotBeNull();
        pending!.RequiresEmailVerification.Should().BeTrue();
        pending.SignedInAtUtc.Should().BeNull();

        var verificationUrl = factory.EmailDeliveryService.GetLatestUrl(selectedEmail, "/auth/verify");
        var verificationUri = new Uri(verificationUrl);
        var query = QueryHelpers.ParseQuery(verificationUri.Query);
        var confirmResponse = await client.PostAsJsonAsync(
            "/auth/email-verification/confirm",
            new ConfirmEmailVerificationRequest(query["email"].ToString(), query["token"].ToString()));

        var confirmBody = await confirmResponse.Content.ReadAsStringAsync();
        confirmResponse.IsSuccessStatusCode.Should().BeTrue(confirmBody);

        var session = await confirmResponse.Content.ReadFromJsonAsync<AuthSessionDto>();
        session.Should().NotBeNull();
        session!.IsEmailVerified.Should().BeTrue();
        session.SignedInAtUtc.Should().NotBeNull();
        return session;
    }

    public static async Task<AuthSessionDto> SignInAsync(HttpClient client, string email, string password)
    {
        var response = await client.PostAsJsonAsync("/auth/sign-in", new SignInRequest(email, password));
        var body = await response.Content.ReadAsStringAsync();
        response.IsSuccessStatusCode.Should().BeTrue(body);

        var session = await response.Content.ReadFromJsonAsync<AuthSessionDto>();
        session.Should().NotBeNull();
        return session!;
    }
}
