using Antital.Application.DTOs.Investors;
using Antital.Domain.Models;

namespace Antital.Application.Features.Investors.GetWalletTransactions;

internal static class WalletTransactionMapper
{
    public static WalletTransactionItemDto ToInvestmentItem(InvestmentOrder order) =>
        new(
            order.Id,
            "Investment",
            "Primary Market Investment",
            $"{order.Offering.Name} {order.Units} units @ ₦{order.SharePrice:N0}/unit",
            order.Subtotal,
            order.PlatformFee,
            order.PaidAt ?? order.UpdatedAt ?? order.CreatedAt,
            "Completed",
            order.Id,
            order.Offering.Slug);
}
