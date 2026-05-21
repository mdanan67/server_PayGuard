using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Options;

namespace server.Services
{
    public interface IEmailService
    {
        Task SendEmailAsync(string toEmail, string subject, string htmlBody);
    }

    public class EmailService : IEmailService
    {
        private readonly EmailSettings _settings;

        public EmailService(IOptions<EmailSettings> options)
        {
            _settings = options.Value;
        }

        public async Task SendEmailAsync(string toEmail, string subject, string htmlBody)
        {
            using var message = new MailMessage();
            message.To.Add(toEmail);
            message.Subject = subject;
            message.Body = htmlBody;
            message.IsBodyHtml = true;

            var senderEmail = (_settings.SenderEmail ?? string.Empty).Trim();
            var username = (_settings.Username ?? string.Empty).Trim();
            var password = (_settings.Password ?? string.Empty).Replace(" ", string.Empty).Trim();

            using var smtp = new SmtpClient(_settings.SmtpHost, _settings.SmtpPort)
            {
                DeliveryMethod = SmtpDeliveryMethod.Network,
                UseDefaultCredentials = false,
                Credentials = new NetworkCredential(username, password),
                EnableSsl = true
            };

            // ensure message From uses trimmed sender email
            message.From = new MailAddress(senderEmail, _settings.SenderName);

            try
            {
                await smtp.SendMailAsync(message);
            }
            catch (SmtpException)
            {
                // rethrow to allow controller to surface detailed message
                throw;
            }
        }
    }
}
