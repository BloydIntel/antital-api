using Antital.Domain.Enums;

namespace Antital.Domain.Interfaces;

public interface IEmailService
{
    Task SendVerificationEmailAsync(string email, string token, CancellationToken cancellationToken);
    Task SendPasswordResetEmailAsync(string email, string token, CancellationToken cancellationToken);
    Task SendUnverifiedOtpEmailAsync(string email, string otp, int validMinutes, CancellationToken cancellationToken);
    Task SendWelcomeEmailAsync(string email, string username, CancellationToken cancellationToken);
    Task SendOnboardingSubmittedEmailAsync(string email, UserTypeEnum userType, CancellationToken cancellationToken);
}
