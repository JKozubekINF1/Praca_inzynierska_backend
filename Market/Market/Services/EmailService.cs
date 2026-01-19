using System.Net;
using System.Net.Mail;
using Market.Interfaces;

namespace Market.Services
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;

        public EmailService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task SendEmailAsync(string to, string subject, string body)
        {
            var email = _configuration["Smtp:Email"];
            var password = _configuration["Smtp:Password"];
            var host = _configuration["Smtp:Host"];
            var port = int.Parse(_configuration["Smtp:Port"]);

            var smtpClient = new SmtpClient(host)
            {
                Port = port,
                Credentials = new NetworkCredential(email, password),
                EnableSsl = true,
            };

            var mailMessage = new MailMessage
            {
                From = new MailAddress(email),
                Subject = subject,
                Body = body,
                IsBodyHtml = true,
            };

            mailMessage.To.Add(to);

            await smtpClient.SendMailAsync(mailMessage);
        }
    }
}