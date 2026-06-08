using Antital.Domain.Enums;
using Antital.Domain.Interfaces;
using System.IO;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Mail;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Http;

namespace Antital.Infrastructure.Services;

public class EmailService : IEmailService
{
    public const string MailgunHttpClientName = "Mailgun";

    private readonly ILogger<EmailService> _logger;
    private readonly EmailSettings _settings;
    private readonly IHostEnvironment _env;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly string _templatesRoot;

    public EmailService(
        ILogger<EmailService> logger,
        IOptions<EmailSettings> options,
        IHostEnvironment env,
        IHttpClientFactory httpClientFactory)
    {
        _logger = logger;
        _settings = options.Value;
        _env = env;
        _httpClientFactory = httpClientFactory;
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

    public Task SendUnverifiedOtpEmailAsync(string email, string otp, int validMinutes, CancellationToken cancellationToken)
    {
        var htmlBody = BuildEmailBody("unverified_otp.html", new Dictionary<string, string>
        {
            { "{{ email }}", email },
            { "{{ otp }}", otp },
            { "{{ valid_minutes }}", validMinutes.ToString() },
            { "{{ base_url }}", _settings.BaseUrl ?? string.Empty }
        },
        fallback: $"""
            <html><body>
            <p>Hello,</p>
            <p>Use this OTP to continue your unverified account request:</p>
            <p><strong>{otp}</strong></p>
            <p>This OTP will expire in {validMinutes} minutes.</p>
            </body></html>
            """);

        LogEmail("Unverified Account OTP", email, "n/a", htmlBody);
        return SendEmailAsync(email, "Your Account OTP", htmlBody, cancellationToken);
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

    public Task SendOnboardingSubmittedEmailAsync(string email, UserTypeEnum userType, CancellationToken cancellationToken)
    {
        var (subject, bodyIntro, bodyDetail) = userType switch
        {
            UserTypeEnum.CorporateInvestor => (
                "Your Antital corporate onboarding has been submitted",
                "Thank you for submitting your corporate onboarding application.",
                "We have received your submission and our compliance team is reviewing your information. We may contact you if additional documentation is required."),
            UserTypeEnum.FundRaiser => (
                "Your Antital fundraiser onboarding has been submitted",
                "Thank you for submitting your fundraiser onboarding application.",
                "We have received your submission and our issuer and compliance teams are reviewing your information. We will contact you by email with next steps."),
            _ => (
                "Your Antital onboarding has been submitted",
                "Thank you for submitting your onboarding application.",
                "We have received your submission and our team is reviewing your information. We will notify you when there are updates.")
        };

        var htmlBody = $"""
            <html><body style="font-family: Arial, sans-serif; color: #111827;">
            <p>Hello,</p>
            <p>{bodyIntro}</p>
            <p>{bodyDetail}</p>
            <p style="color: #6b7280; font-size: 14px;">You received this email for {email}.</p>
            </body></html>
            """;

        LogEmail("Onboarding Submitted", email, _settings.BaseUrl ?? string.Empty, htmlBody);
        return SendEmailAsync(email, subject, htmlBody, cancellationToken);
    }

    private async Task SendEmailAsync(string to, string subject, string htmlBody, CancellationToken cancellationToken)
    {
        if (_settings.UseMailgunApi)
        {
            await SendViaMailgunAsync(to, subject, htmlBody, cancellationToken);
            return;
        }

        if (string.IsNullOrWhiteSpace(_settings.SmtpHost))
        {
            _logger.LogWarning("Email to {Email} skipped: configure Mailgun API or SMTP.", to);
            return;
        }

        await SendViaSmtpAsync(to, subject, htmlBody, cancellationToken);
    }

    private async Task SendViaMailgunAsync(string to, string subject, string htmlBody, CancellationToken cancellationToken)
    {
        var from = string.IsNullOrWhiteSpace(_settings.FromName)
            ? _settings.FromEmail
            : $"{_settings.FromName} <{_settings.FromEmail}>";

        using var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["from"] = from,
            ["to"] = to,
            ["subject"] = subject,
            ["html"] = htmlBody
        });

        var baseUrl = _settings.MailgunApiBaseUrl.TrimEnd('/');
        var requestUri = $"{baseUrl}/v3/{_settings.MailgunDomain}/messages";
        using var request = new HttpRequestMessage(HttpMethod.Post, requestUri)
        {
            Content = content
        };
        request.Headers.Authorization = new AuthenticationHeaderValue(
            "Basic",
            Convert.ToBase64String(Encoding.ASCII.GetBytes($"api:{_settings.MailgunApiKey}")));

        try
        {
            var client = _httpClientFactory.CreateClient(MailgunHttpClientName);
            using var response = await client.SendAsync(request, cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Email sent to {Email} via Mailgun.", to);
                return;
            }

            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogError(
                "Mailgun returned {StatusCode} sending email to {Email}: {ResponseBody}",
                (int)response.StatusCode,
                to,
                responseBody);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {Email} via Mailgun.", to);
        }
    }

    private async Task SendViaSmtpAsync(string to, string subject, string htmlBody, CancellationToken cancellationToken)
    {
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
            _logger.LogInformation("Email sent to {Email} via SMTP.", to);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {Email} via SMTP.", to);
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
