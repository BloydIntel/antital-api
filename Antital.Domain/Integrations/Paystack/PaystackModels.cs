namespace Antital.Domain.Integrations.Paystack;

public record PaystackInitializeRequest(
    string Email,
    int AmountKobo,
    string Reference,
    string CallbackUrl,
    IReadOnlyList<string> Channels);

public record PaystackInitializeResult(
    bool Success,
    string? AuthorizationUrl,
    string? AccessCode,
    string? Reference,
    string? Message);

public record PaystackVerifyResult(
    bool Success,
    string Status,
    string? Channel,
    int AmountKobo,
    string? Message);
