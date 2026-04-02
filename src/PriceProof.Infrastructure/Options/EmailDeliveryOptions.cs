namespace PriceProof.Infrastructure.Options;

public sealed class EmailDeliveryOptions
{
    public const string SectionName = "EmailDelivery";

    public string Provider { get; set; } = "LogOnly";

    public string? FromAddress { get; set; }

    public string FromDisplayName { get; set; } = "PriceProof SA";

    public SmtpOptions Smtp { get; set; } = new();

    public sealed class SmtpOptions
    {
        public string? Host { get; set; }

        public int Port { get; set; } = 587;

        public string? UserName { get; set; }

        public string? Password { get; set; }

        public bool EnableSsl { get; set; } = true;
    }
}
