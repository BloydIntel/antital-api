using Antital.Application.DTOs.Investors;
using BuildingBlocks.Application.Features;

namespace Antital.Application.Features.Investors.PaymentMethods.SetDefaultPaymentMethod;

public record SetDefaultPaymentMethodCommand(int PaymentMethodId) : ICommandQuery<PaymentMethodResponse>;
