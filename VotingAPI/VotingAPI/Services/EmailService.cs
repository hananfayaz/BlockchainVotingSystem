using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using VotingAPI.Services.Interfaces;

namespace VotingAPI.Services
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration configuration;

        public EmailService(IConfiguration configuration)
        {
            this.configuration = configuration;
        }

        public async Task SendEmailAsync(string toEmail, string subject, string body)
        {
            var email = new MimeMessage();

            email.From.Add(MailboxAddress.Parse(configuration["EmailSettings:Email"]!));
            email.To.Add(MailboxAddress.Parse(toEmail));
            email.Subject = subject;

            email.Body = new TextPart("plain")
            {
                Text = body
            };

            using var smtp = new SmtpClient();

            await smtp.ConnectAsync(host: configuration["EmailSettings:Host"]!, port: int.Parse(configuration["EmailSettings:Port"]!), options: SecureSocketOptions.StartTls);

            await smtp.AuthenticateAsync(userName: configuration["EmailSettings:Email"]!, password: configuration["EmailSettings:Password"]!);

            await smtp.SendAsync(email);
            await smtp.DisconnectAsync(true);
        }
    }
}