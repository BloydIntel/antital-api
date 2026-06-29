using Antital.Application.DTOs.Investments;
using Antital.Domain.Enums;
using Swashbuckle.AspNetCore.Filters;

namespace Antital.Application.Features.Investments.Swagger;

public class CreateInvestmentOrderResponseExample : IExamplesProvider<CreateInvestmentOrderResponse>
{
    public CreateInvestmentOrderResponse GetExamples() =>
        new(
            OrderId: 42,
            OfferingId: 7,
            Units: 10,
            SharePrice: 100m,
            Subtotal: 1000m,
            PlatformFeePercent: 2.5m,
            PlatformFee: 25m,
            TotalAmount: 1025m,
            Currency: "NGN",
            Status: nameof(InvestmentOrderStatus.PendingPayment),
            MinInvestment: 1000m,
            MaxInvestment: 50_000m,
            ExpiresAt: DateTime.UtcNow.AddMinutes(30));
}
