using Antital.Application.DTOs.Investments;
using Antital.Application.Features.Investments.Checkout;
using Antital.Application.Features.Investments.Paystack;
using Antital.Domain.Configuration;
using Antital.Domain.Enums;
using Antital.Domain.Integrations.Paystack;
using Antital.Domain.Interfaces;
using Antital.Domain.Models;
using BuildingBlocks.Application.Exceptions;
using BuildingBlocks.Application.Features;
using BuildingBlocks.Domain.Interfaces;
using Microsoft.Extensions.Options;

namespace Antital.Application.Features.Investments.InitializeInvestmentPayment;

public class InitializeInvestmentPaymentCommandHandler(
    IInvestmentCheckoutAccess checkoutAccess,
    IInvestmentOrderRepository orderRepository,
    IUserRepository userRepository,
    IPaystackClient paystackClient,
    IAntitalUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    IOptions<PaystackSettings> paystackOptions
) : ICommandQueryHandler<InitializeInvestmentPaymentCommand, InitializeInvestmentPaymentResponse>
{
    public async Task<Result<InitializeInvestmentPaymentResponse>> Handle(
        InitializeInvestmentPaymentCommand request,
        CancellationToken cancellationToken)
    {
        var (userId, _) = await checkoutAccess.RequireEligibleInvestorAsync(cancellationToken);

        var order = await orderRepository.GetByIdForUserAsync(request.OrderId, userId, cancellationToken);
        if (order == null)
        {
            throw new NotFoundException("Investment order not found.");
        }

        EnsureOrderPayable(order);

        var user = await userRepository.GetByIdAsync(userId, cancellationToken);
        if (user == null)
        {
            throw new NotFoundException("User not found.");
        }

        var settings = paystackOptions.Value;
        if (string.IsNullOrWhiteSpace(settings.SecretKey) || string.IsNullOrWhiteSpace(settings.CallbackUrl))
        {
            throw new BadRequestException(
                "Payment is not configured.",
                new Dictionary<string, string[]> { ["payment"] = ["Paystack is not configured."] });
        }

        var reference = PaystackReferenceGenerator.CreateForOrder(order.Id);
        var amountKobo = PaystackAmountConverter.ToKobo(order.TotalAmount);
        var initializeResult = await paystackClient.InitializeTransactionAsync(
            new PaystackInitializeRequest(
                user.Email,
                amountKobo,
                reference,
                settings.CallbackUrl,
                PaystackChannelMapper.ToPaystackChannels(request.Channel)),
            cancellationToken);

        if (!initializeResult.Success
            || string.IsNullOrWhiteSpace(initializeResult.AuthorizationUrl)
            || string.IsNullOrWhiteSpace(initializeResult.AccessCode)
            || string.IsNullOrWhiteSpace(initializeResult.Reference))
        {
            throw new BadRequestException(
                initializeResult.Message ?? "Unable to initialize payment.",
                new Dictionary<string, string[]> { ["payment"] = ["Paystack initialization failed."] });
        }

        var createdBy = ResolveCreatedBy();
        order.PaymentChannel = request.Channel;
        order.PaystackReference = initializeResult.Reference;
        order.Updated(createdBy);
        await orderRepository.UpdateAsync(order, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        var response = new InitializeInvestmentPaymentResponse(
            initializeResult.AuthorizationUrl,
            initializeResult.AccessCode,
            initializeResult.Reference,
            settings.PublicKey);

        var result = new Result<InitializeInvestmentPaymentResponse>();
        result.AddValue(response);
        result.OK();
        return result;
    }

    private static void EnsureOrderPayable(InvestmentOrder order)
    {
        if (order.Status != InvestmentOrderStatus.PendingPayment)
        {
            throw new BadRequestException(
                "Order is not awaiting payment.",
                new Dictionary<string, string[]> { ["order"] = [$"Order status is {order.Status}."] });
        }

        if (order.ExpiresAt <= DateTime.UtcNow)
        {
            throw new BadRequestException(
                "Order has expired. Create a new order.",
                new Dictionary<string, string[]> { ["order"] = ["Order expired."] });
        }
    }

    private string ResolveCreatedBy() =>
        !string.IsNullOrEmpty(currentUser.UserName)
            ? currentUser.UserName
            : (!string.IsNullOrEmpty(currentUser.IPAddress) ? currentUser.IPAddress : "System");
}
