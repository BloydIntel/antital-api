using Antital.Domain.Interfaces;
using System.IO;
using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Hosting;

namespace Antital.Infrastructure.Services;

public class EmailService : IEmailService
{
    private readonly ILogger<EmailService> _logger;
    private readonly EmailSettings _settings;
    private readonly IHostEnvironment _env;
    private readonly string _templatesRoot;

    public EmailService(ILogger<EmailService> logger, IOptions<EmailSettings> options, IHostEnvironment env)
    {
        _logger = logger;
        _settings = options.Value;
        _env = env;
        _templatesRoot = Path.Combine(AppContext.BaseDirectory, "EmailTemplates");
    }

    public Task SendVerificationEmailAsync(string email, string token, CancellationToken cancellationToken)
    {
        var verificationLink = $"{_settings.BaseUrl}/verify-email?email={Uri.EscapeDataString(email)}&token={Uri.EscapeDataString(token)}";
        var htmlBody = BuildEmailBody("email_verification.html", new Dictionary<string, string>
        {
            { "{{ email }}", email },
            { "{{ verification_link }}", verificationLink },
            { "{{ base_url }}", _settings.BaseUrl ?? string.Empty }
        },
        fallback: $"""
            <html><body>
            <p>Hello,</p>
            <p>Please verify your email address by clicking the link below:</p>
            <p><a href="{verificationLink}">Verify Email</a></p>
            <p>This link will expire in 24 hours.</p>
            </body></html>
            """);

        var isProduction = string.Equals(_env.EnvironmentName, "Production", StringComparison.OrdinalIgnoreCase);
        if (isProduction)
        {
            _logger.LogInformation("Verification email prepared for {Email}.", email);
        }
        else
        {
            var logContent = $"""
                Verification Email
                To: {email}
                Subject: Verify Your Email Address
                Link: {verificationLink}

                {htmlBody}
                """;
            _logger.LogInformation("{EmailContent}", logContent);
        }

        return SendEmailAsync(email, "Verify Your Email Address", htmlBody, cancellationToken);
    }

    private async Task SendEmailAsync(string to, string subject, string htmlBody, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_settings.SmtpHost))
        {
            return;
        }

        using var message = new MailMessage
        {
            From = new MailAddress(_settings.FromEmail, _settings.FromName),
            Subject = subject,
            Body = htmlBody,
            IsBodyHtml = true
        };
        message.To.Add(to);

        using var client = new SmtpClient(_settings.SmtpHost, _settings.SmtpPort)
        {
            EnableSsl = _settings.SmtpSsl || _settings.SmtpTls,
            Credentials = string.IsNullOrWhiteSpace(_settings.SmtpUser)
                ? CredentialCache.DefaultNetworkCredentials
                : new NetworkCredential(_settings.SmtpUser, _settings.SmtpPassword)
        };

        try
        {
            await client.SendMailAsync(message, cancellationToken);
            _logger.LogInformation("Verification email sent to {Email}.", to);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {Email}.", to);
        }
    }

    public string BuildPasswordResetEmail(string email, string username, string link, int validHours)
    {
        return BuildEmailBody("reset_password.html", new Dictionary<string, string>
        {
            { "{{ email }}", email },
            { "{{ username }}", username },
            { "{{ link }}", link },
            { "{{ valid_hours }}", validHours.ToString() },
            { "{{ project_name }}", "Antital" }
        },
        fallback: $"""
            <html><body>
            <p>Hello {username},</p>
            <p>Reset your password using the link below:</p>
            <p><a href="{link}">Reset password</a></p>
            <p>This link will expire in {validHours} hours.</p>
            </body></html>
            """);
    }

    public string BuildNewAccountEmail(string email, string username, string password, string link, string projectName)
    {
        return BuildEmailBody("new_account.html", new Dictionary<string, string>
        {
            { "{{ email }}", email },
            { "{{ username }}", username },
            { "{{ password }}", password },
            { "{{ link }}", link },
            { "{{ project_name }}", projectName }
        },
        fallback: $"""
            <html><body>
            <p>Welcome {username},</p>
            <p>Your account has been created.</p>
            <p>Username: {username}<br/>Password: {password}</p>
            <p><a href="{link}">Go to dashboard</a></p>
            </body></html>
            """);
    }

    private string BuildEmailBody(string templateFile, Dictionary<string, string> replacements, string fallback)
    {
        try
        {
            var path = Path.Combine(_templatesRoot, templateFile);
            if (File.Exists(path))
            {
                var template = File.ReadAllText(path);
                foreach (var kvp in replacements)
                {
                    template = template.Replace(kvp.Key, kvp.Value);
                }
                return template;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load email template {TemplateFile}. Falling back to plain text.", templateFile);
        }

        return fallback;
    }
}
