using Antital.Application.DTOs.Investors;
using BuildingBlocks.Application.Features;

namespace Antital.Application.Features.Investors.PaymentMethods.GetPaymentMethods;

public record GetPaymentMethodsQuery : ICommandQuery<PaymentMethodsResponse>;
