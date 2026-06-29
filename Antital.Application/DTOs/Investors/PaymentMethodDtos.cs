namespace Antital.Application.DTOs.Investors;

public record PaymentMethodItemDto(
    int Id,
    string Type,
    string Title,
    string Subtitle,
    bool IsDefault,
    bool IsVerified,
    DateTime AddedAt);

public record PaymentMethodsResponse(IReadOnlyList<PaymentMethodItemDto> Items);

public record AddPaymentMethodRequest(
    string Type,
    string Title,
    string ProviderName,
    string Last4,
    bool SetAsDefault = false);

public record PaymentMethodResponse(PaymentMethodItemDto Item);
