using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MimeKit;

namespace CRMS.infrastructure.Services
{
    // SMTP credentials live in appsettings.json under "Smtp" (same hardcoded-
    // per-environment pattern as ConnectionStrings/TenantCredentials/Jwt) —
    // nothing here is a per-tenant setting, every shop's notifications go out
    // through this one configured account.
    public class SmtpEmailService : IEmailService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<SmtpEmailService> _logger;

        public SmtpEmailService(IConfiguration configuration, ILogger<SmtpEmailService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public async Task SendAsync(string toEmail, string toName, string subject, string body)
        {
            // Email is optional on a Customer record — silently skip rather
            // than log a warning for every walk-in who only gave a phone number.
            if (string.IsNullOrWhiteSpace(toEmail))
                return;

            var smtp = _configuration.GetSection("Smtp");
            string? host = smtp["Host"];
            if (string.IsNullOrWhiteSpace(host))
            {
                _logger.LogWarning("Smtp:Host is not configured — skipping notification email to {ToEmail}.", toEmail);
                return;
            }

            try
            {
                var message = new MimeMessage();
                message.From.Add(new MailboxAddress(smtp["FromName"] ?? "CRMS Repair Shop", smtp["FromEmail"] ?? smtp["Username"]));
                message.To.Add(new MailboxAddress(toName, toEmail));
                message.Subject = subject;
                message.Body = new TextPart("plain") { Text = body };

                int port = int.TryParse(smtp["Port"], out int parsedPort) ? parsedPort : 587;
                bool useStartTls = !bool.TryParse(smtp["EnableSsl"], out bool enableSsl) || enableSsl;

                using var client = new SmtpClient();
                await client.ConnectAsync(host, port, useStartTls ? SecureSocketOptions.StartTls : SecureSocketOptions.None);

                string? username = smtp["Username"];
                if (!string.IsNullOrWhiteSpace(username))
                    await client.AuthenticateAsync(username, smtp["Password"]);

                await client.SendAsync(message);
                await client.DisconnectAsync(true);
            }
            catch (Exception ex)
            {
                // Never let a notification failure surface as a failed ticket
                // operation — the shop's own workflow keeps working even if
                // SMTP is unreachable or misconfigured.
                _logger.LogError(ex, "Failed to send notification email to {ToEmail}.", toEmail);
            }
        }
    }
}
