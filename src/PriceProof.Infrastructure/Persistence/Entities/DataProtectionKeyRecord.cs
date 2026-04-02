namespace PriceProof.Infrastructure.Persistence.Entities;

public sealed class DataProtectionKeyRecord
{
    public int Id { get; private set; }

    public string FriendlyName { get; private set; } = string.Empty;

    public string Xml { get; private set; } = string.Empty;

    public DateTimeOffset CreatedUtc { get; private set; }

    public DateTimeOffset UpdatedUtc { get; private set; }

    public static DataProtectionKeyRecord Create(string friendlyName, string xml, DateTimeOffset now)
    {
        return new DataProtectionKeyRecord
        {
            FriendlyName = friendlyName.Trim(),
            Xml = xml.Trim(),
            CreatedUtc = now,
            UpdatedUtc = now
        };
    }
}
