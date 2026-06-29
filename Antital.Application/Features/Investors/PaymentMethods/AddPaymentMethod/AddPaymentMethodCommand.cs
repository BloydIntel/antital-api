using Antital.Application.DTOs.Investors;
using BuildingBlocks.Application.Features;

namespace Antital.Application.Features.Investors.PaymentMethods.AddPaymentMethod;

public record AddPaymentMethodCommand(
    string Type,
    string Title,
    string ProviderName,
    string Last4,
    bool SetAsDefault) : ICommandQuery<PaymentMethodResponse>;
