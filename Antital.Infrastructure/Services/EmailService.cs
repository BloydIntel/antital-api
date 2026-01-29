using Antital.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Antital.Infrastructure.Services;

public class EmailService : IEmailService
{
    private readonly ILogger<EmailService> _logger;
    private readonly EmailSettings _settings;

    public EmailService(ILogger<EmailService> logger, IOptions<EmailSettings> options)
    {
        _logger = logger;
        _settings = options.Value;
    }

    public Task SendVerificationEmailAsync(string email, string token, CancellationToken cancellationToken)
    {
        // For now, log the email content to console/file
        // In production, this would send an actual email via SMTP, SendGrid, etc.
        
        var verificationLink = $"{_settings.BaseUrl}/verify-email?email={Uri.EscapeDataString(email)}&token={Uri.EscapeDataString(token)}";
        
        var emailContent = $"""
            ============================================
            EMAIL VERIFICATION
            ============================================
            From: {_settings.FromName} <{_settings.FromEmail}>
            To: {email}
            Subject: Verify Your Email Address
            
            Please click the following link to verify your email address:
            {verificationLink}
            
            This link will expire in 24 hours.
            
            If you did not create an account, please ignore this email.
            ============================================
            """;

        _logger.LogInformation("Verification Email:\n{EmailContent}", emailContent);
        Console.WriteLine(emailContent);

        // TODO: Implement actual SMTP email sending when EmailSettings are configured
        // if (!string.IsNullOrEmpty(_settings.SmtpHost))
        // {
        //     // Send via SMTP using _settings.SmtpHost, _settings.SmtpPort, etc.
        // }

        return Task.CompletedTask;
    }
}
