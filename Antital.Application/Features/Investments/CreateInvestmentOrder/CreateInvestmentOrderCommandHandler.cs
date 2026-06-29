using Antital.Domain.Configuration;
using Antital.Application.DTOs.Investments;
using Antital.Application.Features.Investments.Checkout;
using Antital.Domain.Enums;
using Antital.Domain.Interfaces;
using Antital.Domain.Models;
using BuildingBlocks.Application.Exceptions;
using BuildingBlocks.Application.Features;
using BuildingBlocks.Domain.Interfaces;
using Microsoft.Extensions.Options;

namespace Antital.Application.Features.Investments.CreateInvestmentOrder;

public class CreateInvestmentOrderCommandHandler(
    IInvestmentCheckoutAccess checkoutAccess,
    IInvestmentOfferingRepository offeringRepository,
    IInvestmentOrderRepository orderRepository,
    IAntitalUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    IOptions<PaystackSettings> paystackOptions
) : ICommandQueryHandler<CreateInvestmentOrderCommand, CreateInvestmentOrderResponse>
{
    public async Task<Result<CreateInvestmentOrderResponse>> Handle(
        CreateInvestmentOrderCommand request,
        CancellationToken cancellationToken)
    {
        var (userId, _) = await checkoutAccess.RequireEligibleInvestorAsync(cancellationToken);

        var offering = await offeringRepository.GetPublishedShellByIdAsync(request.OfferingId, cancellationToken);
        if (offering?.Funding == null || offering.DealTerms == null)
        {
            throw new NotFoundException("Investment offering not found.");
        }

        ValidateOfferingOpen(offering);

        var settings = paystackOptions.Value;
        var sharePrice = offering.Funding.SharePrice;
        var (subtotal, platformFee, totalAmount) = InvestmentOrderCalculator.Calculate(
            request.Units,
            sharePrice,
            settings.PlatformFeePercent);

        ValidateAmounts(subtotal, offering.Funding.MinInvestment, offering.Funding.MaxInvestment);

        var createdBy = ResolveCreatedBy();
        var expiresAt = DateTime.UtcNow.AddMinutes(settings.OrderExpiryMinutes);

        var order = await ResolveOrCreatePendingOrderAsync(
            userId,
            offering.Id,
            request.Units,
            sharePrice,
            subtotal,
            platformFee,
            totalAmount,
            settings.PlatformFeePercent,
            expiresAt,
            createdBy,
            cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        var response = MapToResponse(order, offering.Funding.MinInvestment, offering.Funding.MaxInvestment);
        var result = new Result<CreateInvestmentOrderResponse>();
        result.AddValue(response);
        result.OK();
        return result;
    }

    private async Task<InvestmentOrder> ResolveOrCreatePendingOrderAsync(
        int userId,
        int offeringId,
        int units,
        decimal sharePrice,
        decimal subtotal,
        decimal platformFee,
        decimal totalAmount,
        decimal platformFeePercent,
        DateTime expiresAt,
        string createdBy,
        CancellationToken cancellationToken)
    {
        var existing = await orderRepository.GetPendingByUserAndOfferingAsync(userId, offeringId, cancellationToken);
        if (existing != null)
        {
            if (existing.ExpiresAt <= DateTime.UtcNow)
            {
                existing.Status = InvestmentOrderStatus.Expired;
                existing.Updated(createdBy);
                await orderRepository.UpdateAsync(existing, cancellationToken);
            }
            else
            {
                ApplyOrderAmounts(existing, units, sharePrice, subtotal, platformFee, totalAmount, platformFeePercent, expiresAt, createdBy);
                await orderRepository.UpdateAsync(existing, cancellationToken);
                return existing;
            }
        }

        var order = new InvestmentOrder
        {
            UserId = userId,
            OfferingId = offeringId,
            Status = InvestmentOrderStatus.PendingPayment,
            Currency = "NGN",
        };
        ApplyOrderAmounts(order, units, sharePrice, subtotal, platformFee, totalAmount, platformFeePercent, expiresAt, createdBy);
        order.Created(createdBy);
        await orderRepository.AddAsync(order, cancellationToken);
        return order;
    }

    private static void ApplyOrderAmounts(
        InvestmentOrder order,
        int units,
        decimal sharePrice,
        decimal subtotal,
        decimal platformFee,
        decimal totalAmount,
        decimal platformFeePercent,
        DateTime expiresAt,
        string createdBy)
    {
        order.Units = units;
        order.SharePrice = sharePrice;
        order.Subtotal = subtotal;
        order.PlatformFeePercent = platformFeePercent;
        order.PlatformFee = platformFee;
        order.TotalAmount = totalAmount;
        order.ExpiresAt = expiresAt;
        order.PaystackReference = null;
        order.PaymentChannel = null;
        order.Updated(createdBy);
    }

    private static void ValidateOfferingOpen(InvestmentOffering offering)
    {
        if (offering.Status != OfferingStatus.Published)
        {
            throw new BadRequestException(
                "This offering is not open for investment.",
                new Dictionary<string, string[]> { ["offering"] = ["Offering is not published."] });
        }

        if (offering.DealTerms!.Deadline <= DateTime.UtcNow)
        {
            throw new BadRequestException(
                "This offering has closed.",
                new Dictionary<string, string[]> { ["offering"] = ["Funding deadline has passed."] });
        }
    }

    private static void ValidateAmounts(decimal subtotal, decimal minInvestment, decimal maxInvestment)
    {
        if (subtotal < minInvestment)
        {
            throw new BadRequestException(
                $"Minimum investment is ₦{minInvestment:N0}.",
                new Dictionary<string, string[]>
                {
                    ["units"] = [$"Investment amount must be at least ₦{minInvestment:N0}."],
                });
        }

        if (subtotal > maxInvestment)
        {
            throw new BadRequestException(
                $"Maximum investment per order is ₦{maxInvestment:N0}.",
                new Dictionary<string, string[]>
                {
                    ["units"] = [$"Investment amount cannot exceed ₦{maxInvestment:N0}."],
                });
        }
    }

    private string ResolveCreatedBy() =>
        !string.IsNullOrEmpty(currentUser.UserName)
            ? currentUser.UserName
            : (!string.IsNullOrEmpty(currentUser.IPAddress) ? currentUser.IPAddress : "System");

    private static CreateInvestmentOrderResponse MapToResponse(
        InvestmentOrder order,
        decimal minInvestment,
        decimal maxInvestment) =>
        new(
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
            minInvestment,
            maxInvestment,
            order.ExpiresAt!.Value);
}
