using System.Security.Cryptography;
using System.Text;

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
}
