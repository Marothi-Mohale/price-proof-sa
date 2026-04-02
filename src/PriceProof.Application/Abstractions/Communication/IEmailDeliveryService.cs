namespace PriceProof.Application.Abstractions.Communication;

public interface IEmailDeliveryService
{
    Task SendAsync(EmailMessage message, CancellationToken cancellationToken);
}

public sealed record EmailRecipient(string Email, string? DisplayName = null);

public sealed record EmailMessage(
    EmailRecipient To,
    string Subject,
    string PlainTextBody);
