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

    public Task SendPasswordResetEmailAsync(string email, string token, CancellationToken cancellationToken)
    {
        // Token is opaque and already contains user identity, no need to expose email in URL
        var resetLink = $"{_settings.BaseUrl}/reset-password?token={Uri.EscapeDataString(token)}";
        var htmlBody = BuildEmailBody("reset_password.html", new Dictionary<string, string>
        {
            { "{{ email }}", email },
            { "{{ reset_link }}", resetLink },
            { "{{ base_url }}", _settings.BaseUrl ?? string.Empty },
            { "{{ valid_hours }}", "1" },
            { "{{ project_name }}", "Antital" }
        },
        fallback: $"""
            <html><body>
            <p>Hello,</p>
            <p>You requested a password reset. Use the link below:</p>
            <p><a href=\"{resetLink}\">Reset Password</a></p>
            <p>This link will expire in 1 hour.</p>
            </body></html>
            """);

        LogEmail("Password Reset", email, resetLink, htmlBody);

        return SendEmailAsync(email, "Reset Your Password", htmlBody, cancellationToken);
    }

    public Task SendWelcomeEmailAsync(string email, string username, CancellationToken cancellationToken)
    {
        var htmlBody = BuildEmailBody("new_account.html", new Dictionary<string, string>
        {
            { "{{ email }}", email },
            { "{{ username }}", username },
            { "{{ link }}", _settings.BaseUrl ?? string.Empty },
            { "{{ project_name }}", "Antital" }
        },
        fallback: $"""
            <html><body>
            <p>Welcome {username},</p>
            <p>Your account has been created.</p>
            <p>Username: {username}</p>
            <p><a href="{_settings.BaseUrl}">Go to dashboard</a></p>
            </body></html>
            """);

        LogEmail("Welcome", email, _settings.BaseUrl ?? string.Empty, htmlBody);

        return SendEmailAsync(email, "Welcome to Antital", htmlBody, cancellationToken);
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

    private void LogEmail(string kind, string email, string link, string htmlBody)
    {
        var isProduction = string.Equals(_env.EnvironmentName, "Production", StringComparison.OrdinalIgnoreCase);
        if (isProduction)
        {
            _logger.LogInformation("{Kind} email prepared for {Email}.", kind, email);
        }
        else
        {
            var logContent = $"""
                {kind} Email
                To: {email}
                Subject: {kind}
                Link: {link}

                {htmlBody}
                """;
            _logger.LogInformation("{EmailContent}", logContent);
        }
    }

    public string BuildPasswordResetEmail(string email, string username, string link, int validHours)
    {
        // TODO: Wire into the password-reset flow once implemented so this template is actually sent.
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
