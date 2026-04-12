using EmailNotification.Services;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;

public class EmailService : IEmailService
{
    private readonly SmtpSettings _settings;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IOptions<SmtpSettings> settings, ILogger<EmailService> logger)
    {
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task SendEmailAsync(EmailRequest request)
    {
        try
        {
            var email = new MimeMessage();
            email.From.Add(new MailboxAddress(_settings.FromName, _settings.FromEmail));

            email.To.AddRange(request.To.Select(x => MailboxAddress.Parse(x)));

            if (request.Cc != null)
                email.Cc.AddRange(request.Cc.Select(x => MailboxAddress.Parse(x)));

            if (request.Bcc != null)
                email.Bcc.AddRange(request.Bcc.Select(x => MailboxAddress.Parse(x)));

            email.Subject = request.Subject;

            var builder = new BodyBuilder
            {
                HtmlBody = request.Body
            };

            // Attachments
            if (request.Attachments != null)
            {
                foreach (var file in request.Attachments)
                {
                    if (File.Exists(file))
                        builder.Attachments.Add(file);
                }
            }

            email.Body = builder.ToMessageBody();

            using var smtp = new SmtpClient();
            await smtp.ConnectAsync(_settings.Host,_settings.Port,SecureSocketOptions.StartTls);
            await smtp.AuthenticateAsync(_settings.Username, _settings.Password);
            await smtp.SendAsync(email);
            await smtp.DisconnectAsync(true);

            _logger.LogInformation("Email sent successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending email");
            throw;
        }
    }
}