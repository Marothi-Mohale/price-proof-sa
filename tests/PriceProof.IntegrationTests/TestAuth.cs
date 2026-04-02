using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
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

        var session = admin
            ? await SignInAsync(client, PriceProofApiFactory.BootstrapAdminEmail, PriceProofApiFactory.BootstrapAdminPassword)
            : await SignUpAsync(client);

        return (client, session);
    }

    public static async Task<AuthSessionDto> SignUpAsync(HttpClient client, string? email = null, string? displayName = null)
    {
        var unique = Guid.NewGuid().ToString("N");
        var response = await client.PostAsJsonAsync(
            "/auth/sign-up",
            new SignUpRequest(
                email ?? $"user-{unique}@priceproof.test",
                displayName ?? $"Test User {unique[..6]}",
                DefaultPassword));

        var body = await response.Content.ReadAsStringAsync();
        response.IsSuccessStatusCode.Should().BeTrue(body);

        var session = await response.Content.ReadFromJsonAsync<AuthSessionDto>();
        session.Should().NotBeNull();
        return session!;
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
