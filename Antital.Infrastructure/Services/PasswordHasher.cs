using Antital.Domain.Interfaces;

namespace Antital.Infrastructure.Services;

public class PasswordHasher : IPasswordHasher
{
    public string HashPassword(string password)
    {
        if (string.IsNullOrEmpty(password))
            throw new ArgumentException("Password cannot be null or empty.", nameof(password));

        // BCrypt automatically handles salting
        return BCrypt.Net.BCrypt.HashPassword(password, BCrypt.Net.BCrypt.GenerateSalt(12));
    }

    public bool VerifyPassword(string password, string passwordHash)
    {
        if (string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(passwordHash))
            return false;

        // Fast-fail if the stored hash is not a BCrypt hash (avoids noisy exceptions/logs)
        if (!passwordHash.StartsWith("$2", StringComparison.Ordinal))
            return false;

        try
        {
            return BCrypt.Net.BCrypt.Verify(password, passwordHash);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[PasswordHasher] BCrypt.Verify failed: {ex.Message}");
            return false;
        }
    }
}
