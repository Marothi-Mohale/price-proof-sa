using System.Text.RegularExpressions;
using PriceProof.Application.Abstractions.Communication;

namespace PriceProof.IntegrationTests.Fakes;

public sealed class FakeEmailDeliveryService : IEmailDeliveryService
{
    private static readonly Regex UrlRegex = new(@"https?://\S+", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private readonly object _sync = new();
    private readonly List<EmailMessage> _messages = [];

    public Task SendAsync(EmailMessage message, CancellationToken cancellationToken)
    {
        lock (_sync)
        {
            _messages.Add(message);
        }

        return Task.CompletedTask;
    }

    public string GetLatestUrl(string email, string pathSegment)
    {
        lock (_sync)
        {
            var body = _messages
                .Where(message => string.Equals(message.To.Email, email, StringComparison.OrdinalIgnoreCase))
                .Select(message => message.PlainTextBody)
                .Reverse()
                .FirstOrDefault(body => body.Contains(pathSegment, StringComparison.OrdinalIgnoreCase));

            if (body is null)
            {
                throw new InvalidOperationException($"No email containing '{pathSegment}' was captured for {email}.");
            }

            var match = UrlRegex.Match(body);
            if (!match.Success)
            {
                throw new InvalidOperationException($"No URL was captured in the email body for {email}.");
            }

            return match.Value.Trim();
        }
    }
}
