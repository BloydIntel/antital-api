using System.Text.Json.Serialization;
using Refit;

namespace Antital.Infrastructure.Integrations.Paystack.Refit;

public interface IPaystackApi
{
    [Post("/transaction/initialize")]
    Task<HttpResponseMessage> InitializeTransactionAsync(
        [Body] PaystackInitializePayload request,
        CancellationToken cancellationToken = default);

    [Get("/transaction/verify/{reference}")]
    Task<HttpResponseMessage> VerifyTransactionAsync(string reference, CancellationToken cancellationToken = default);
}

public sealed record PaystackInitializePayload(
    [property: JsonPropertyName("email")] string Email,
    [property: JsonPropertyName("amount")] int Amount,
    [property: JsonPropertyName("reference")] string Reference,
    [property: JsonPropertyName("callback_url")] string CallbackUrl,
    [property: JsonPropertyName("channels")] IReadOnlyCollection<string>? Channels,
    [property: JsonPropertyName("currency")] string Currency,
    [property: JsonPropertyName("metadata")] PaystackInitializeMetadata Metadata);

public sealed record PaystackInitializeMetadata(
    [property: JsonPropertyName("orderReference")] string OrderReference);

internal sealed class PaystackEnvelope<T>
{
    public bool Status { get; set; }
    public string? Message { get; set; }
    public T? Data { get; set; }
}

internal sealed class PaystackInitializeData
{
    public string? AuthorizationUrl { get; set; }
    public string? AccessCode { get; set; }
    public string? Reference { get; set; }
}

internal sealed class PaystackVerifyData
{
    public string Status { get; set; } = string.Empty;
    public string? Channel { get; set; }
    public int Amount { get; set; }
}
