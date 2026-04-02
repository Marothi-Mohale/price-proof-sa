namespace PriceProof.Infrastructure.Options;

public sealed class SessionAuthOptions
{
    public const string SectionName = "SessionAuth";

    public string CookieName { get; set; } = "priceproof.session";

    public int SessionLifetimeHours { get; set; } = 12;
}
