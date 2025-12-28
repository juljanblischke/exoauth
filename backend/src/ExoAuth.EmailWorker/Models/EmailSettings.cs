namespace ExoAuth.EmailWorker.Models;

public sealed class EmailSettings
{
    public string Provider { get; set; } = "SMTP";
    public string SmtpHost { get; set; } = "localhost";
    public int SmtpPort { get; set; } = 587;
    public string? SmtpUsername { get; set; }
    public string? SmtpPassword { get; set; }
    public bool SmtpUseSsl { get; set; } = true;
    public string FromEmail { get; set; } = "noreply@exoauth.com";
    public string FromName { get; set; } = "ExoAuth";
}
