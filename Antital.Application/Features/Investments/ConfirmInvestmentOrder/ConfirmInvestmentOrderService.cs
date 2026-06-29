using Antital.Domain.Enums;
using Antital.Domain.Interfaces;
using Antital.Domain.Models;

namespace Antital.Application.Features.Investments.ConfirmInvestmentOrder;

public class ConfirmInvestmentOrderService(IInvestmentOrderRepository orderRepository) : IConfirmInvestmentOrderService
{
    public async Task<bool> TryFulfillAsync(
        InvestmentOrder order,
        string actor,
        CancellationToken cancellationToken = default)
    {
        if (order.Status != InvestmentOrderStatus.Paid)
        {
            return false;
        }

        if (order.InvestorHoldingId.HasValue)
        {
            return true;
        }

        var funding = await orderRepository.GetOfferingFundingForUpdateAsync(order.OfferingId, cancellationToken);
        if (funding == null)
        {
            return false;
        }

        var investedAt = order.PaidAt ?? DateTime.UtcNow;
        var existingHolding = await orderRepository.GetHoldingByUserAndOfferingAsync(
            order.UserId,
            order.OfferingId,
            cancellationToken);

        InvestorHolding holding;
        if (existingHolding == null)
        {
            holding = new InvestorHolding
            {
                UserId = order.UserId,
                OfferingId = order.OfferingId,
                InvestedAmount = order.Subtotal,
                CurrentValue = order.Subtotal,
                Returns = 0m,
                UnitHolding = order.Units,
                InvestedAt = investedAt,
            };
            holding.Created(actor);
            await orderRepository.AddInvestorHoldingAsync(holding, cancellationToken);
            funding.InvestorCount += 1;
        }
        else
        {
            existingHolding.InvestedAmount += order.Subtotal;
            existingHolding.CurrentValue += order.Subtotal;
            existingHolding.UnitHolding += order.Units;
            existingHolding.Updated(actor);
            await orderRepository.UpdateInvestorHoldingAsync(existingHolding, cancellationToken);
            holding = existingHolding;
        }

        funding.RaisedAmount += order.Subtotal;
        funding.Updated(actor);
        await orderRepository.UpdateOfferingFundingAsync(funding, cancellationToken);

        order.InvestorHolding = holding;
        order.Updated(actor);
        await orderRepository.UpdateAsync(order, cancellationToken);

        return true;
    }
}
