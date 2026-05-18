namespace Antital.Application.DTOs.Onboarding;

public record FundRaiserPaymentPayload(
    string PaymentMethod,
    string PaymentReference,
    string PaymentStatus,
    bool ApplicationFeePaid
);
