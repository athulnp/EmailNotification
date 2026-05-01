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
        _logger.LogInformation("Starting email send process. From: {FromEmail}, To: {ToCount}, Cc: {CcCount}, Bcc: {BccCount}",
            _settings.FromEmail, request.To?.Count ?? 0, request.Cc?.Count ?? 0, request.Bcc?.Count ?? 0);

        try
        {
            var email = new MimeMessage();
            email.From.Add(new MailboxAddress(_settings.FromName, _settings.FromEmail));

            email.To.AddRange(request.To.Select(x => MailboxAddress.Parse(x)));
            _logger.LogDebug("Added {Count} recipients to email", request.To.Count);

            if (request.Cc != null)
            {
                email.Cc.AddRange(request.Cc.Select(x => MailboxAddress.Parse(x)));
                _logger.LogDebug("Added {Count} CC recipients to email", request.Cc.Count);
            }

            if (request.Bcc != null)
            {
                email.Bcc.AddRange(request.Bcc.Select(x => MailboxAddress.Parse(x)));
                _logger.LogDebug("Added {Count} BCC recipients to email", request.Bcc.Count);
            }

            email.Subject = request.Subject;
            _logger.LogDebug("Email subject set: {Subject}", request.Subject);

            var builder = new BodyBuilder
            {
                HtmlBody = request.Body
            };
            _logger.LogDebug("Email body builder created. Body length: {BodyLength} characters", request.Body?.Length ?? 0);

            // Attachments
            if (request.Attachments != null)
            {
                _logger.LogInformation("Processing {Count} attachments", request.Attachments.Count);
                var validAttachments = 0;
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
                    validAttachments++;
                    _logger.LogDebug("Attachment added: {FilePath}", fullPath);
                }
                _logger.LogInformation("Successfully added {ValidCount}/{TotalCount} attachments", validAttachments, request.Attachments.Count);
            }

            email.Body = builder.ToMessageBody();

            _logger.LogInformation("Connecting to SMTP server: {Host}:{Port}", _settings.Host, _settings.Port);
            var connectStopwatch = System.Diagnostics.Stopwatch.StartNew();
            
            using var smtp = new SmtpClient();
            // Port 587 uses STARTTLS, Port 465 uses SSL
            var secureOptions = _settings.Port == 465 ? SecureSocketOptions.SslOnConnect : SecureSocketOptions.StartTls;
            await smtp.ConnectAsync(_settings.Host, _settings.Port, secureOptions, cancellationToken).ConfigureAwait(false);
            connectStopwatch.Stop();
            _logger.LogInformation("SMTP connection established. Duration: {Duration}ms", connectStopwatch.ElapsedMilliseconds);

            _logger.LogDebug("Authenticating with SMTP server");
            var authStopwatch = System.Diagnostics.Stopwatch.StartNew();
            await smtp.AuthenticateAsync(_settings.Username, _settings.Password, cancellationToken).ConfigureAwait(false);
            authStopwatch.Stop();
            _logger.LogInformation("SMTP authentication successful. Duration: {Duration}ms", authStopwatch.ElapsedMilliseconds);

            _logger.LogInformation("Sending email message");
            var sendStopwatch = System.Diagnostics.Stopwatch.StartNew();
            await smtp.SendAsync(email, cancellationToken).ConfigureAwait(false);
            sendStopwatch.Stop();
            _logger.LogInformation("Email sent successfully. Duration: {Duration}ms", sendStopwatch.ElapsedMilliseconds);

            await smtp.DisconnectAsync(true, cancellationToken).ConfigureAwait(false);
            _logger.LogDebug("SMTP connection closed");

            _logger.LogInformation("Email send process completed successfully. Total recipients: {TotalRecipients}",
                (request.To?.Count ?? 0) + (request.Cc?.Count ?? 0) + (request.Bcc?.Count ?? 0));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending email. SMTP Host: {Host}, Port: {Port}, From: {FromEmail}",
                _settings.Host, _settings.Port, _settings.FromEmail);
            throw new EmailSendingException("Failed to send email", ex);
        }
    }
}