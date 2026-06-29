using Antital.Domain.Models;

namespace Antital.Application.Features.Investments.ConfirmInvestmentOrder;

public interface IConfirmInvestmentOrderService
{
    Task<bool> TryFulfillAsync(InvestmentOrder order, string actor, CancellationToken cancellationToken = default);
}
