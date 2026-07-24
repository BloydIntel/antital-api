namespace Antital.Application.DTOs.Onboarding;

public record ApplicationFeeStatusResponse(
    decimal Amount,
    string Currency,
    bool ApplicationFeePaid,
    string? PaymentMethod,
    string? PaymentReference,
    string? PaymentStatus
);

public record InitializeApplicationFeePaymentRequest(string Channel);

public record InitializeApplicationFeePaymentResponse(
    string AuthorizationUrl,
    string AccessCode,
    string Reference,
    string PublicKey,
    decimal Amount,
    string Currency
);

public record VerifyApplicationFeePaymentRequest(string? Reference = null);
