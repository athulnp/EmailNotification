using EmailNotification.Exceptions;
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

    public async Task SendEmailAsync(EmailRequest request, CancellationToken cancellationToken = default)
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
                    var fullPath = Path.GetFullPath(file);
                    if (!File.Exists(fullPath))
                    {
                        _logger.LogWarning("Attachment file not found: {FilePath}", fullPath);
                        continue;
                    }

                    // Validate path is within allowed directory (current directory or subdirectories)
                    var currentDir = Directory.GetCurrentDirectory();
                    if (!fullPath.StartsWith(currentDir, StringComparison.OrdinalIgnoreCase))
                    {
                        _logger.LogWarning("Attachment path outside allowed directory: {FilePath}", fullPath);
                        continue;
                    }

                    builder.Attachments.Add(fullPath);
                }
            }

            email.Body = builder.ToMessageBody();

            using var smtp = new SmtpClient();
            // Port 587 uses STARTTLS, Port 465 uses SSL
            var secureOptions = _settings.Port == 465 ? SecureSocketOptions.SslOnConnect : SecureSocketOptions.StartTls;
            await smtp.ConnectAsync(_settings.Host, _settings.Port, secureOptions, cancellationToken).ConfigureAwait(false);
            await smtp.AuthenticateAsync(_settings.Username, _settings.Password, cancellationToken).ConfigureAwait(false);
            await smtp.SendAsync(email, cancellationToken).ConfigureAwait(false);
            await smtp.DisconnectAsync(true, cancellationToken).ConfigureAwait(false);

            _logger.LogInformation("Email sent successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending email");
            throw new EmailSendingException("Failed to send email", ex);
        }
    }
}