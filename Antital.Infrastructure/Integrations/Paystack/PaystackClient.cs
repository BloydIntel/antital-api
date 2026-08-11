using System.Text.Json;
using System.Text.Json.Serialization;
using Antital.Domain.Configuration;
using Antital.Domain.Integrations.Paystack;
using Antital.Domain.Interfaces;
using Antital.Infrastructure.Integrations.Paystack.Refit;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Antital.Infrastructure.Integrations.Paystack;

public class PaystackClient(
    IPaystackApi paystackApi,
    IOptions<PaystackSettings> options,
    ILogger<PaystackClient> logger) : IPaystackClient
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    public async Task<PaystackInitializeResult> InitializeTransactionAsync(
        PaystackInitializeRequest request,
        CancellationToken cancellationToken = default)
    {
        EnsureSecretKeyConfigured();

        var payload = new PaystackInitializePayload(
            request.Email,
            request.AmountKobo,
            request.Reference,
            request.CallbackUrl,
            request.Channels,
            "NGN",
            new PaystackInitializeMetadata(request.Reference));

        using var response = await paystackApi.InitializeTransactionAsync(payload, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            logger.LogWarning("Paystack initialize failed with status {StatusCode}: {Body}", response.StatusCode, body);
            return new PaystackInitializeResult(false, null, null, null, "Unable to initialize payment.");
        }

        var parsed = JsonSerializer.Deserialize<PaystackEnvelope<PaystackInitializeData>>(body, JsonOptions);
        if (parsed is not { Status: true, Data: not null })
        {
            return new PaystackInitializeResult(false, null, null, null, parsed?.Message ?? "Unable to initialize payment.");
        }

        return new PaystackInitializeResult(
            true,
            parsed.Data.AuthorizationUrl,
            parsed.Data.AccessCode,
            parsed.Data.Reference,
            parsed.Message);
    }

    public async Task<PaystackVerifyResult> VerifyTransactionAsync(
        string reference,
        CancellationToken cancellationToken = default)
    {
        EnsureSecretKeyConfigured();

        using var response = await paystackApi.VerifyTransactionAsync(reference, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            logger.LogWarning("Paystack verify failed with status {StatusCode}: {Body}", response.StatusCode, body);
            return new PaystackVerifyResult(false, "failed", null, 0, "Unable to verify payment.");
        }

        var parsed = JsonSerializer.Deserialize<PaystackEnvelope<PaystackVerifyData>>(body, JsonOptions);
        if (parsed?.Data == null)
        {
            return new PaystackVerifyResult(false, "failed", null, 0, parsed?.Message ?? "Unable to verify payment.");
        }

        return new PaystackVerifyResult(
            parsed.Status && string.Equals(parsed.Data.Status, "success", StringComparison.OrdinalIgnoreCase),
            parsed.Data.Status,
            parsed.Data.Channel,
            parsed.Data.Amount,
            parsed.Message);
    }

    private void EnsureSecretKeyConfigured()
    {
        if (string.IsNullOrWhiteSpace(options.Value.SecretKey))
        {
            throw new InvalidOperationException("Paystack secret key is not configured.");
        }
    }
}
