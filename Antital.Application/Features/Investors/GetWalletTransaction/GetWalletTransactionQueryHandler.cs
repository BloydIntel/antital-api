using Antital.Application.DTOs.Investors;
using Antital.Domain.Enums;
using Antital.Domain.Interfaces;
using Antital.Domain.Models;
using BuildingBlocks.Application.Exceptions;
using BuildingBlocks.Application.Features;

namespace Antital.Application.Features.Investors.GetWalletTransaction;

public class GetWalletTransactionQueryHandler(
    IInvestorUserAccess investorUserAccess,
    IInvestmentOrderRepository orderRepository
) : ICommandQueryHandler<GetWalletTransactionQuery, WalletTransactionInvoiceResponse>
{
    public async Task<Result<WalletTransactionInvoiceResponse>> Handle(
        GetWalletTransactionQuery request,
        CancellationToken cancellationToken)
    {
        var (userId, user) = await investorUserAccess.RequireAuthenticatedUserAsync(cancellationToken);

        var order = await orderRepository.GetByIdForUserAsync(request.TransactionId, userId, cancellationToken);
        if (order == null || order.Status != InvestmentOrderStatus.Paid)
        {
            throw new NotFoundException("Wallet transaction not found.");
        }

        var response = WalletTransactionInvoiceMapper.ToInvoice(order, user);
        var result = new Result<WalletTransactionInvoiceResponse>();
        result.AddValue(response);
        result.OK();
        return result;
    }
}

internal static class WalletTransactionInvoiceMapper
{
    public static WalletTransactionInvoiceResponse ToInvoice(InvestmentOrder order, User user)
    {
        var paidAt = order.PaidAt ?? order.UpdatedAt ?? order.CreatedAt;

        return new WalletTransactionInvoiceResponse(
            order.Id,
            paidAt,
            paidAt,
            FormatPaymentMethod(order.PaymentChannel),
            order.PaystackReference,
            new WalletTransactionBillToDto(
                ResolveBillToName(user),
                user.Email,
                string.IsNullOrWhiteSpace(user.PhoneNumber) ? null : user.PhoneNumber),
            new WalletTransactionDetailsDto("Investment", "Completed"),
            new WalletTransactionBreakdownDto(
                "Primary Market Investment",
                order.Offering.Name,
                order.Offering.Category,
                order.Units,
                order.SharePrice,
                order.Subtotal,
                order.PlatformFeePercent,
                order.PlatformFee,
                order.TotalAmount));
    }

    private static string ResolveBillToName(User user)
    {
        if (!string.IsNullOrWhiteSpace(user.PreferredName))
        {
            return user.PreferredName.Trim();
        }

        var fullName = $"{user.FirstName} {user.LastName}".Trim();
        return string.IsNullOrWhiteSpace(fullName) ? user.Email : fullName;
    }

    private static string FormatPaymentMethod(PaymentChannel? channel) =>
        channel switch
        {
            PaymentChannel.Card => "Card",
            PaymentChannel.Transfer => "Bank Transfer",
            PaymentChannel.Opay => "Opay",
            _ => "Paystack",
        };
}
