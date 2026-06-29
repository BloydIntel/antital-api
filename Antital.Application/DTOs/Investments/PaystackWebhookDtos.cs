namespace Antital.Application.DTOs.Investments;

public record PaystackChargeDataDto(
    string Reference,
    int Amount,
    string Channel,
    string Status);

public record PaystackWebhookPayloadDto(
    string Event,
    PaystackChargeDataDto Data);
