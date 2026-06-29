using BuildingBlocks.Application.Features;

namespace Antital.Application.Features.Investors.PaymentMethods.DeletePaymentMethod;

public record DeletePaymentMethodCommand(int PaymentMethodId) : ICommandQuery;
