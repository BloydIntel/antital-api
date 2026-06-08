namespace Antital.Infrastructure.Services;

public class EmailSettings
{
    /// <summary>Mailgun sending domain (e.g. www.thekyub.com). Used with HTTPS API on Render.</summary>
    public string MailgunDomain { get; set; } = string.Empty;

    /// <summary>Mailgun private API key. Set via environment variable — never commit.</summary>
    public string MailgunApiKey { get; set; } = string.Empty;

    /// <summary>Mailgun API base URL. Defaults to US region.</summary>
    public string MailgunApiBaseUrl { get; set; } = "https://api.mailgun.net";

    public string SmtpHost { get; set; } = string.Empty;
    public int SmtpPort { get; set; } = 587;
    public string SmtpUser { get; set; } = string.Empty;
    public string SmtpPassword { get; set; } = string.Empty;
    public bool SmtpSsl { get; set; } = false;
    public bool SmtpTls { get; set; } = true;
    public string FromEmail { get; set; } = string.Empty;
    public string FromName { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = string.Empty;

    public bool UseMailgunApi =>
        !string.IsNullOrWhiteSpace(MailgunApiKey) && !string.IsNullOrWhiteSpace(MailgunDomain);
}
