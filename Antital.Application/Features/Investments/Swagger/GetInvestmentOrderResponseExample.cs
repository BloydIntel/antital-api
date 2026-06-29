using Antital.Application.DTOs.Investments;
using Antital.Domain.Enums;
using Swashbuckle.AspNetCore.Filters;

namespace Antital.Application.Features.Investments.Swagger;

public class GetInvestmentOrderResponseExample : IExamplesProvider<GetInvestmentOrderResponse>
{
    public GetInvestmentOrderResponse GetExamples() =>
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
            Status: nameof(InvestmentOrderStatus.Paid),
            PaystackReference: "ANT-ORD-42-a1b2c3d4e5f6",
            ExpiresAt: DateTime.UtcNow.AddMinutes(30),
            PaidAt: DateTime.UtcNow,
            InvestorHoldingId: 15);
}
