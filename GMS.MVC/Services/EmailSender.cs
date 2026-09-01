using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;

namespace GMS.MVC.Services {
    /// <summary>Sends transactional mail — today only the password-reset link.</summary>
    public interface IEmailSender {
        Task SendAsync(string toAddress, string toName, string subject, string htmlBody, string textBody);
    }

    /// <summary>Bound From The <c>Email</c> Configuration Section.</summary>
    public class EmailOptions {
        public const string SectionName = "Email";

        /// <summary>
        /// False Puts The App In Development Mode For Mail: Nothing Is Sent And The Reset Link Is
        /// Written To The Log Instead, So The Flow Can Be Exercised Without An SMTP Server.
        /// </summary>
        public bool Enabled { get; set; }

        public string Host { get; set; } = string.Empty;
        public int Port { get; set; } = 587;

        /// <summary>STARTTLS On 587 Is The Usual Choice; Set False Only For Implicit TLS On 465.</summary>
        public bool UseStartTls { get; set; } = true;

        public string UserName { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string FromAddress { get; set; } = string.Empty;
        public string FromName { get; set; } = "Power Fitness";

        public bool IsConfigured =>
            Enabled && !string.IsNullOrWhiteSpace(Host) && !string.IsNullOrWhiteSpace(FromAddress);
    }

    public class SmtpEmailSender(IOptions<EmailOptions> options, ILogger<SmtpEmailSender> logger) : IEmailSender {
        private readonly EmailOptions _options = options.Value;

        public async Task SendAsync(string toAddress, string toName, string subject, string htmlBody, string textBody) {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(_options.FromName, _options.FromAddress));
            message.To.Add(new MailboxAddress(toName, toAddress));
            message.Subject = subject;
            message.Body = new BodyBuilder { HtmlBody = htmlBody, TextBody = textBody }.ToMessageBody();

            using var client = new SmtpClient();
            await client.ConnectAsync(
                _options.Host,
                _options.Port,
                _options.UseStartTls ? SecureSocketOptions.StartTls : SecureSocketOptions.SslOnConnect);

            if (!string.IsNullOrWhiteSpace(_options.UserName))
                await client.AuthenticateAsync(_options.UserName, _options.Password);

            await client.SendAsync(message);
            await client.DisconnectAsync(true);

            logger.LogInformation("Sent \"{Subject}\" to {Recipient}.", subject, toAddress);
        }
    }

    /// <summary>
    /// Used When No SMTP Server Is Configured. Writes The Message To The Log So The Reset Flow Can
    /// Be Completed From The Console, And Never Renders It To The Browser — Showing A Reset Link On
    /// Screen Would Hand Anyone Who Knows An Email Address A Way Into That Account.
    /// </summary>
    public class LoggingEmailSender(ILogger<LoggingEmailSender> logger) : IEmailSender {
        public Task SendAsync(string toAddress, string toName, string subject, string htmlBody, string textBody) {
            logger.LogWarning(
                "Email is not configured, so nothing was sent. The message for {Recipient} was:\n" +
                "Subject: {Subject}\n{Body}\n" +
                "Configure the Email section to send this for real.",
                toAddress, subject, textBody);

            return Task.CompletedTask;
        }
    }
}
