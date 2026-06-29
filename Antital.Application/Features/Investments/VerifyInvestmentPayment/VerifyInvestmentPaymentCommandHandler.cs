using System.Text.Json;
using Antital.Application.DTOs.Investments;
using Antital.Application.Features.Investments.Checkout;
using Antital.Application.Features.Investments.ProcessPaystackWebhook;
using Antital.Domain.Enums;
using Antital.Domain.Interfaces;
using BuildingBlocks.Application.Exceptions;
using BuildingBlocks.Application.Features;

namespace Antital.Application.Features.Investments.VerifyInvestmentPayment;

/// <summary>
/// Confirms payment via Paystack verify API. Use when webhooks cannot reach localhost during local dev.
/// </summary>
public class VerifyInvestmentPaymentCommandHandler(
    IInvestmentCheckoutAccess checkoutAccess,
    IInvestmentOrderRepository orderRepository,
    IPaystackClient paystackClient,
    IInvestmentPaymentConfirmationService paymentConfirmationService
) : ICommandQueryHandler<VerifyInvestmentPaymentCommand, GetInvestmentOrderResponse>
{
    public async Task<Result<GetInvestmentOrderResponse>> Handle(
        VerifyInvestmentPaymentCommand request,
        CancellationToken cancellationToken)
    {
        var (userId, _) = await checkoutAccess.RequireEligibleInvestorAsync(cancellationToken);

        var order = await orderRepository.GetByIdForUserAsync(request.OrderId, userId, cancellationToken);
        if (order == null)
        {
            throw new NotFoundException("Investment order not found.");
        }

        if (order.Status == InvestmentOrderStatus.Paid)
        {
            return Success(order);
        }

        if (string.IsNullOrWhiteSpace(order.PaystackReference))
        {
            throw new BadRequestException(
                "Payment has not been initialized for this order.",
                new Dictionary<string, string[]> { ["order"] = ["Missing Paystack reference."] });
        }

        if (order.Status != InvestmentOrderStatus.PendingPayment)
        {
            throw new BadRequestException(
                "Order is not awaiting payment.",
                new Dictionary<string, string[]> { ["order"] = [$"Order status is {order.Status}."] });
        }

        var verifyResult = await paystackClient.VerifyTransactionAsync(order.PaystackReference, cancellationToken);
        if (!verifyResult.Success)
        {
            throw new BadRequestException(
                verifyResult.Message ?? "Payment is not complete yet.",
                new Dictionary<string, string[]> { ["payment"] = ["Paystack verification failed."] });
        }

        var rawPayload = JsonSerializer.Serialize(new
        {
            @event = "charge.success",
            data = new
            {
                reference = order.PaystackReference,
                amount = verifyResult.AmountKobo,
                channel = verifyResult.Channel,
                status = verifyResult.Status,
            },
        });

        await paymentConfirmationService.TryConfirmSuccessfulChargeAsync(
            order.PaystackReference,
            verifyResult.AmountKobo,
            verifyResult.Channel,
            rawPayload,
            cancellationToken);

        var updated = await orderRepository.GetByIdForUserAsync(request.OrderId, userId, cancellationToken)
            ?? throw new NotFoundException("Investment order not found.");

        return Success(updated);
    }

    private static Result<GetInvestmentOrderResponse> Success(Domain.Models.InvestmentOrder order)
    {
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
