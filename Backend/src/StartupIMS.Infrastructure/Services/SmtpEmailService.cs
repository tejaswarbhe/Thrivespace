using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;
using StartupIMS.Shared.Settings;

namespace StartupIMS.Infrastructure.Services;

public class SmtpEmailService : IEmailService
{
    private readonly EmailSettings _settings;
    private readonly ILogger<SmtpEmailService> _logger;

    public SmtpEmailService(IOptions<EmailSettings> settings, ILogger<SmtpEmailService> logger)
    {
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task SendAsync(string toAddress, string subject, string htmlBody)
    {
        if (!_settings.Enabled)
        {
            _logger.LogInformation("Email disabled (Email:Enabled=false) - would have sent {Subject} to {To}", subject, toAddress);
            return;
        }

        try
        {
            var message = new MimeMessage();
            var fromAddress = string.IsNullOrWhiteSpace(_settings.FromAddress) ? "noreply@startupims.com" : _settings.FromAddress;
            message.From.Add(new MailboxAddress(_settings.FromName, fromAddress));
            message.To.Add(MailboxAddress.Parse(toAddress));
            message.Subject = subject;
            message.Body = new TextPart("html") { Text = htmlBody };

            using var client = new SmtpClient();
            await client.ConnectAsync(
                _settings.SmtpHost,
                _settings.SmtpPort,
                _settings.UseSsl ? SecureSocketOptions.Auto : SecureSocketOptions.None);

            if (!string.IsNullOrEmpty(_settings.SmtpUsername))
                await client.AuthenticateAsync(_settings.SmtpUsername, _settings.SmtpPassword);

            await client.SendAsync(message);
            await client.DisconnectAsync(true);

            _logger.LogInformation("Sent email {Subject} to {To}", subject, toAddress);
        }
        catch (Exception ex)
        {
            // Deliberately swallowed - see IEmailService's doc comment. A
            // broken mail server should never fail a registration or a
            // funding approval; it should just mean nobody got notified.
            _logger.LogWarning(ex, "Failed to send email {Subject} to {To}", subject, toAddress);
        }
    }
}
