using System.Security.Cryptography;
using System.Text;
using System.Globalization;

namespace Antital.Application.Common.Security;

public static class TokenGenerator
{
    public static string GenerateSecureToken(int byteLength = 32)
    {
        using var rng = RandomNumberGenerator.Create();
        var bytes = new byte[byteLength];
        rng.GetBytes(bytes);
        return Convert.ToBase64String(bytes).Replace("+", "-").Replace("/", "_").Replace("=", "");
    }

    public static string HashToken(string token)
    {
        using var sha = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(token);
        var hash = sha.ComputeHash(bytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    public static bool VerifyTokenHash(string token, string storedHash)
    {
        var computedHash = HashToken(token);
        var computedBytes = Encoding.UTF8.GetBytes(computedHash);
        var storedBytes = Encoding.UTF8.GetBytes(storedHash);
        return CryptographicOperations.FixedTimeEquals(computedBytes, storedBytes);
    }

    public static string GenerateSixDigitOtp()
    {
        var value = RandomNumberGenerator.GetInt32(0, 1_000_000);
        return value.ToString("D6", CultureInfo.InvariantCulture);
    }
}
