namespace PriceProofSA.Infrastructure.Options;

public sealed class AuthOptions
{
    public const string SectionName = "Auth";

    public string SchemeName { get; set; } = "SessionToken";

    public List<string> AdminEmails { get; set; } = [];
}
