using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using System.Net.Mail;
using System.Net;


namespace diplomaProject
{
    public class EmailSender : IEmailSender 
    {
        private readonly IConfiguration _config;

        public EmailSender(IConfiguration config)
        {
            _config = config;
        }

        public async Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            var smtpUser = _config["EmailSettings:smtpUser"]?.Trim();
            var pw = _config["EmailSettings:Password"];
            var host = _config["EmailSettings:Host"];
            var port = int.Parse(_config["EmailSettings:Port"] ?? "587");
            var fromEmail = _config["EmailSettings:FromEmail"];

            using var client = new SmtpClient(host, port)
            {
                // 1. Авторизація 
                Credentials = new NetworkCredential(smtpUser, pw),
                EnableSsl = true
            };

            // 2. Створення листа
            using var message = new MailMessage(from: fromEmail, to: email)
            {
                Subject = subject,
                Body = htmlMessage,
                IsBodyHtml = true
            };

            await client.SendMailAsync(message);
            //var message = new MailMessage(from: mail, to: email, subject, htmlMessage)
            //{
            //    IsBodyHtml = true
            //};
           
        }
    }
}
