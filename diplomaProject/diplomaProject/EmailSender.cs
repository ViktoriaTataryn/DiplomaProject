using System.Net;
using System.Net.Mail;
using Microsoft.AspNetCore.Identity.UI.Services;

namespace diplomaProject;

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

        using var client = new SmtpClient(host, port);
        // 1. Авторизація 
        client.Credentials = new NetworkCredential(smtpUser, pw);
        client.EnableSsl = true;

        // 2. Створення листа
        using var message = new MailMessage(fromEmail!, email);
        message.Subject = subject;
        message.Body = htmlMessage;
        message.IsBodyHtml = true;

        await client.SendMailAsync(message);
      
    }
}