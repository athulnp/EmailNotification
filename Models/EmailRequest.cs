using System.ComponentModel.DataAnnotations;

public class EmailRequest
{
    [Required, EmailAddressList]
    public List<string> To { get; set; } = new();

    [EmailAddressList]
    public List<string>? Cc { get; set; }
    [EmailAddressList]
    public List<string>? Bcc { get; set; }

    [Required]
    public string Subject { get; set; } = string.Empty;

    [Required]
    public string Body { get; set; } = string.Empty;

    public List<string>? Attachments { get; set; } // file paths
}

public class EmailAddressListAttribute : ValidationAttribute
{
    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value is null)
            return ValidationResult.Success;

        if (value is string email)
        {
            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                if (addr.Address == email)
                    return ValidationResult.Success;
            }
            catch
            {
                return new ValidationResult("Invalid email address format");
            }
        }
        else if (value is List<string> emails)
        {
            foreach (var emailAddress in emails)
            {
                try
                {
                    var addr = new System.Net.Mail.MailAddress(emailAddress);
                    if (addr.Address != emailAddress)
                        return new ValidationResult($"Invalid email address format: {emailAddress}");
                }
                catch
                {
                    return new ValidationResult($"Invalid email address format: {emailAddress}");
                }
            }
            return ValidationResult.Success;
        }

        return new ValidationResult("Invalid email address format");
    }
}