using Antital.Application.DTOs.Investments;
using Antital.Application.Features.Investments.Checkout;
using Antital.Domain.Interfaces;
using BuildingBlocks.Application.Exceptions;
using BuildingBlocks.Application.Features;

namespace Antital.Application.Features.Investments.GetInvestmentOrder;

public class GetInvestmentOrderQueryHandler(
    IInvestmentCheckoutAccess checkoutAccess,
    IInvestmentOrderRepository orderRepository
) : ICommandQueryHandler<GetInvestmentOrderQuery, GetInvestmentOrderResponse>
{
    public async Task<Result<GetInvestmentOrderResponse>> Handle(
        GetInvestmentOrderQuery request,
        CancellationToken cancellationToken)
    {
        var (userId, _) = await checkoutAccess.RequireEligibleInvestorAsync(cancellationToken);

        var order = await orderRepository.GetByIdForUserAsync(request.OrderId, userId, cancellationToken);
        if (order == null)
        {
            throw new NotFoundException("Investment order not found.");
        }

        var response = new GetInvestmentOrderResponse(
            order.Id,
            order.OfferingId,
            order.Units,
            order.SharePrice,
            order.Subtotal,
            order.PlatformFeePercent,
            order.PlatformFee,
            order.TotalAmount,
            order.Currency,
            order.Status.ToString(),
            order.PaystackReference,
            order.ExpiresAt,
            order.PaidAt,
            order.InvestorHoldingId);

        var result = new Result<GetInvestmentOrderResponse>();
        result.AddValue(response);
        result.OK();
        return result;
    }
}
