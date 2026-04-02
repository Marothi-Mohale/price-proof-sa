namespace PriceProof.Infrastructure.Options;

public sealed class BootstrapAdminOptions
{
    public const string SectionName = "BootstrapAdmin";

    public string? Email { get; set; }

    public string? DisplayName { get; set; }

    public string? Password { get; set; }
}
