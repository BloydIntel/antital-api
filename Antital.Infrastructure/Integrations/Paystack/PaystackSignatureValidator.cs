using System.Security.Cryptography;
using System.Text;
using Antital.Domain.Configuration;
using Microsoft.Extensions.Options;

namespace Antital.Infrastructure.Integrations.Paystack;

public class PaystackSignatureValidator(IOptions<PaystackSettings> options)
{
    public bool IsValid(string payload, string? signatureHeader)
    {
        if (string.IsNullOrWhiteSpace(signatureHeader))
        {
            return false;
        }

        var secret = ResolveSigningSecret();
        if (string.IsNullOrWhiteSpace(secret))
        {
            return false;
        }

        var hash = HMACSHA512.HashData(Encoding.UTF8.GetBytes(secret), Encoding.UTF8.GetBytes(payload));
        var computed = Convert.ToHexString(hash).ToLowerInvariant();
        var computedBytes = Encoding.UTF8.GetBytes(computed);
        var signatureBytes = Encoding.UTF8.GetBytes(signatureHeader.Trim().ToLowerInvariant());

        if (computedBytes.Length != signatureBytes.Length)
        {
            return false;
        }

        return CryptographicOperations.FixedTimeEquals(computedBytes, signatureBytes);
    }

    private string ResolveSigningSecret()
    {
        var settings = options.Value;
        // Paystack does not expose a separate webhook secret in the dashboard;
        // HMAC signatures use the secret key when WebhookSecret is empty.
        return !string.IsNullOrWhiteSpace(settings.WebhookSecret)
            ? settings.WebhookSecret
            : settings.SecretKey;
    }
}
