using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Options;
using PriceProof.Application.Abstractions.Communication;
using PriceProof.Application.Common.Exceptions;
using PriceProof.Infrastructure.Options;

namespace PriceProof.Infrastructure.Communication;

internal sealed class SmtpEmailDeliveryService : IEmailDeliveryService
{
    private readonly EmailDeliveryOptions _options;

    public SmtpEmailDeliveryService(IOptions<EmailDeliveryOptions> options)
    {
        _options = options.Value;
    }

    public async Task SendAsync(EmailMessage message, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_options.FromAddress))
        {
            throw new ServiceUnavailableException("Account email delivery is not configured.");
        }

        using var smtpClient = new SmtpClient();
        if (!string.IsNullOrWhiteSpace(_options.Smtp.Host))
        {
            smtpClient.Host = _options.Smtp.Host.Trim();
        }

        smtpClient.Port = _options.Smtp.Port;
        smtpClient.EnableSsl = _options.Smtp.EnableSsl;

        if (!string.IsNullOrWhiteSpace(_options.Smtp.UserName))
        {
            smtpClient.Credentials = new NetworkCredential(
                _options.Smtp.UserName.Trim(),
                _options.Smtp.Password ?? string.Empty);
        }
        else
        {
            smtpClient.UseDefaultCredentials = true;
        }

        using var mailMessage = new MailMessage
        {
            From = new MailAddress(_options.FromAddress.Trim(), _options.FromDisplayName),
            Subject = message.Subject,
            Body = message.PlainTextBody,
            IsBodyHtml = false
        };
        mailMessage.To.Add(new MailAddress(message.To.Email, message.To.DisplayName));

        try
        {
            await smtpClient.SendMailAsync(mailMessage, cancellationToken);
        }
        catch (SmtpException)
        {
            throw new ServiceUnavailableException("Account email delivery is currently unavailable. Please try again later.");
        }
        catch (InvalidOperationException)
        {
            throw new ServiceUnavailableException("Account email delivery is currently unavailable. Please try again later.");
        }
    }
}
