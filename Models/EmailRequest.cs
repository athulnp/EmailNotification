using System.ComponentModel.DataAnnotations;

public class EmailRequest
{
    [Required]
    public List<string> To { get; set; } = new();

    public List<string>? Cc { get; set; }
    public List<string>? Bcc { get; set; }

    [Required]
    public string Subject { get; set; } = string.Empty;

    [Required]
    public string Body { get; set; } = string.Empty;

    public List<string>? Attachments { get; set; } // file paths
}