namespace Antital.Domain.Interfaces;

public interface IEmailService
{
    Task SendVerificationEmailAsync(string email, string token, CancellationToken cancellationToken);
    Task SendPasswordResetEmailAsync(string email, string token, CancellationToken cancellationToken);
    Task SendWelcomeEmailAsync(string email, string username, CancellationToken cancellationToken);
}
