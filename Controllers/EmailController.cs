using EmailNotification.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
public class EmailController : ControllerBase
{
    private readonly IEmailService _emailService;
    private readonly ILogger<EmailController> _logger;

    public EmailController(IEmailService emailService, ILogger<EmailController> logger)
    {
        _emailService = emailService;
        _logger = logger;
    }

    [HttpPost("send-email")]
    public async Task<IActionResult> SendEmail([FromBody] EmailRequest request, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Email send request received. Recipients: {RecipientCount}, Subject: {Subject}", 
            request.To?.Count ?? 0, request.Subject);

        if (!ModelState.IsValid)
        {
            _logger.LogWarning("Invalid model state for email request. Errors: {Errors}", 
                string.Join(", ", ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage))));
            return BadRequest(ModelState);
        }

        try
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            await _emailService.SendEmailAsync(request, cancellationToken);
            stopwatch.Stop();

            _logger.LogInformation("Email sent successfully. Recipients: {RecipientCount}, Duration: {Duration}ms", 
                request.To?.Count ?? 0, stopwatch.ElapsedMilliseconds);

            return Ok(new { message = "Email sent successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email. Recipients: {RecipientCount}, Subject: {Subject}", 
                request.To?.Count ?? 0, request.Subject);
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpGet("User")]
    [Authorize]
    public async Task<IActionResult> SendEmailWithAuth()
    {
        string userId = User.Identity?.Name ?? "Unknown";
        _logger.LogInformation("User authentication check. User: {UserId}", userId);
        return Ok(new { message = $"Authenticated user: {userId}" });
    }
}