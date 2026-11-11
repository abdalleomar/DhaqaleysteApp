using System.Net;
using System.Net.Mail;
using DhaqaleysteApi.Options;
using Microsoft.Extensions.Options;

namespace DhaqaleysteApi.Services
{
    public interface IEmailSender
    {
        /// <summary>Sends a plain-text email. Returns false when it could not be sent.</summary>
        Task<bool> SendAsync(string to, string subject, string body, CancellationToken ct = default);
    }

    /// <summary>Sends email through the SMTP server in configuration (Email:*).</summary>
    public class SmtpEmailSender : IEmailSender
    {
        private readonly EmailOptions _options;
        private readonly IWebHostEnvironment _env;
        private readonly ILogger<SmtpEmailSender> _logger;

        public SmtpEmailSender(IOptions<EmailOptions> options, IWebHostEnvironment env, ILogger<SmtpEmailSender> logger)
        {
            _options = options.Value;
            _env = env;
            _logger = logger;
        }

        public async Task<bool> SendAsync(string to, string subject, string body, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(_options.SmtpHost) || string.IsNullOrWhiteSpace(_options.FromAddress))
            {
                if (_env.IsDevelopment())
                {
                    // Development only: no mail server yet, so show the message in the API window.
                    _logger.LogWarning("Email not configured. Development copy of email to {To}:\n{Subject}\n{Body}", to, subject, body);
                    return true;
                }
                _logger.LogError("Email is not configured (Email:SmtpHost / Email:FromAddress); could not send '{Subject}'", subject);
                return false;
            }

            try
            {
                using var message = new MailMessage(new MailAddress(_options.FromAddress, _options.FromName), new MailAddress(to))
                {
                    Subject = subject,
                    Body = body
                };
                using var client = new SmtpClient(_options.SmtpHost, _options.SmtpPort)
                {
                    EnableSsl = _options.EnableSsl,
                    Credentials = string.IsNullOrEmpty(_options.Username)
                        ? null
                        : new NetworkCredential(_options.Username, _options.Password)
                };
                await client.SendMailAsync(message, ct);
                return true;
            }
            catch (Exception ex) when (ex is SmtpException or InvalidOperationException or FormatException)
            {
                _logger.LogError(ex, "Sending email '{Subject}' failed", subject);
                return false;
            }
        }
    }
}
