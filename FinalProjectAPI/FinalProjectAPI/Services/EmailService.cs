using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace FinalProjectAPI.Services
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;

        public EmailService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task<bool> SendEmailAsync(string toEmail, string subject, string htmlBody)
        {
            return await SendEmailAsync(toEmail, string.Empty, subject, htmlBody);
        }

        public async Task<bool> SendEmailAsync(string toEmail, string toName, string subject, string htmlBody)
        {
            try
            {
                var emailSettings = _configuration.GetSection("EmailSettings");

                var smtpServer = emailSettings["SmtpServer"]
                    ?? throw new InvalidOperationException("SMTP Server is not configured.");
                var port = int.Parse(emailSettings["Port"] ?? "587");
                var senderEmail = emailSettings["SenderEmail"]
                    ?? throw new InvalidOperationException("Sender Email is not configured.");
                var senderName = emailSettings["SenderName"] ?? "Caramellize Bakery";
                var username = emailSettings["Username"]
                    ?? throw new InvalidOperationException("SMTP Username is not configured.");
                var password = emailSettings["Password"]
                    ?? throw new InvalidOperationException("SMTP Password is not configured.");

                var message = new MimeMessage();
                message.From.Add(new MailboxAddress(senderName, senderEmail));
                message.To.Add(new MailboxAddress(toName, toEmail));
                message.Subject = subject;

                var bodyBuilder = new BodyBuilder
                {
                    HtmlBody = htmlBody
                };
                message.Body = bodyBuilder.ToMessageBody();

                using var client = new SmtpClient();
                await client.ConnectAsync(smtpServer, port, SecureSocketOptions.StartTls);
                await client.AuthenticateAsync(username, password);
                await client.SendAsync(message);
                await client.DisconnectAsync(true);

                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
