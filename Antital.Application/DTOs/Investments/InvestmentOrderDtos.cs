namespace Antital.Application.DTOs.Investments;

public record CreateInvestmentOrderRequest(int Units);

public record CreateInvestmentOrderResponse(
    int OrderId,
    int OfferingId,
    int Units,
    decimal SharePrice,
    decimal Subtotal,
    decimal PlatformFeePercent,
    decimal PlatformFee,
    decimal TotalAmount,
    string Currency,
    string Status,
    decimal MinInvestment,
    decimal MaxInvestment,
    DateTime ExpiresAt);

public record InitializeInvestmentPaymentRequest(string Channel);

public record InitializeInvestmentPaymentResponse(
    string AuthorizationUrl,
    string AccessCode,
    string Reference,
    string PublicKey);

public record GetInvestmentOrderResponse(
    int OrderId,
    int OfferingId,
    int Units,
    decimal SharePrice,
    decimal Subtotal,
    decimal PlatformFeePercent,
    decimal PlatformFee,
    decimal TotalAmount,
    string Currency,
    string Status,
    string? PaystackReference,
    DateTime? ExpiresAt,
    DateTime? PaidAt,
    int? InvestorHoldingId);
