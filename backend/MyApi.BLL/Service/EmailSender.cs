using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Configuration;

namespace MyApi.BLL.Service;

public class EmailSender : IEmailSender
{
    private readonly IConfiguration _configuration;

    public EmailSender(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public Task SendEmailAsync(
        string email,
        string subject,
        string htmlMessage)
    {
        var senderEmail =
            _configuration["EmailSettings:SenderEmail"]
            ?? throw new InvalidOperationException(
                "Email sender address is missing."
            );

        var appPassword =
            _configuration["EmailSettings:AppPassword"]
            ?? throw new InvalidOperationException(
                "Email app password is missing."
            );

        var client = new SmtpClient("smtp.gmail.com", 587)
        {
            EnableSsl = true,
            UseDefaultCredentials = false,
            Credentials = new NetworkCredential(
                senderEmail,
                appPassword
            )
        };

        var mailMessage = new MailMessage(
            from: senderEmail,
            to: email,
            subject: subject,
            body: htmlMessage
        )
        {
            IsBodyHtml = true
        };

        return client.SendMailAsync(mailMessage);
    }
}