using PriceProof.Application.Abstractions.Communication;
using Microsoft.Extensions.Logging;

namespace PriceProof.Infrastructure.Communication;

internal sealed class LoggingEmailDeliveryService : IEmailDeliveryService
{
    private readonly ILogger<LoggingEmailDeliveryService> _logger;

    public LoggingEmailDeliveryService(ILogger<LoggingEmailDeliveryService> logger)
    {
        _logger = logger;
    }

    public Task SendAsync(EmailMessage message, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Email delivery simulated for {Email}. Subject: {Subject}. Body: {Body}",
            message.To.Email,
            message.Subject,
            message.PlainTextBody);
        return Task.CompletedTask;
    }
}
