namespace EmailNotification.Services
{
    public interface IEmailService
    {
        Task SendEmailAsync(EmailRequest request, CancellationToken cancellationToken = default);
    }
}
