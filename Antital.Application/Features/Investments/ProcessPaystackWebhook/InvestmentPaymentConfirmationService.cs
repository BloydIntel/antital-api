using Antital.Application.Features.Investments.ConfirmInvestmentOrder;
using Antital.Application.Features.Investments.Paystack;
using Antital.Domain.Enums;
using Antital.Domain.Interfaces;
using Antital.Domain.Models;
using BuildingBlocks.Domain.Interfaces;

namespace Antital.Application.Features.Investments.ProcessPaystackWebhook;

public interface IInvestmentPaymentConfirmationService
{
    Task<bool> TryConfirmSuccessfulChargeAsync(
        string reference,
        int amountKobo,
        string? channel,
        string rawPayload,
        CancellationToken cancellationToken = default);

    Task<bool> TryMarkFailedChargeAsync(
        string reference,
        string rawPayload,
        CancellationToken cancellationToken = default);
}

public class InvestmentPaymentConfirmationService(
    IInvestmentOrderRepository orderRepository,
    IConfirmInvestmentOrderService confirmInvestmentOrderService,
    IAntitalUnitOfWork unitOfWork,
    ICurrentUser currentUser
) : IInvestmentPaymentConfirmationService
{
    public async Task<bool> TryConfirmSuccessfulChargeAsync(
        string reference,
        int amountKobo,
        string? channel,
        string rawPayload,
        CancellationToken cancellationToken = default)
    {
        var order = await orderRepository.GetByPaystackReferenceAsync(reference, cancellationToken);
        if (order == null)
        {
            return false;
        }

        if (order.Status == InvestmentOrderStatus.Paid)
        {
            if (order.InvestorHoldingId.HasValue)
            {
                return true;
            }

            var actor = ResolveActor();
            await confirmInvestmentOrderService.TryFulfillAsync(order, actor, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return order.InvestorHoldingId.HasValue;
        }

        var existingTransaction = await orderRepository.GetPaymentTransactionByReferenceAsync(reference, cancellationToken);
        if (existingTransaction?.Status == PaymentTransactionStatus.Success)
        {
            return true;
        }

        var expectedKobo = PaystackAmountConverter.ToKobo(order.TotalAmount);
        if (amountKobo != expectedKobo)
        {
            return false;
        }

        var createdBy = ResolveActor();
        order.Status = InvestmentOrderStatus.Paid;
        order.PaidAt = DateTime.UtcNow;
        order.Updated(createdBy);
        await orderRepository.UpdateAsync(order, cancellationToken);

        if (existingTransaction == null)
        {
            var transaction = new PaymentTransaction
            {
                OrderId = order.Id,
                Reference = reference,
                Channel = channel,
                Status = PaymentTransactionStatus.Success,
                RawPayloadJson = rawPayload,
                ProcessedAt = DateTime.UtcNow,
            };
            transaction.Created(createdBy);
            await orderRepository.AddPaymentTransactionAsync(transaction, cancellationToken);
        }
        else
        {
            existingTransaction.Status = PaymentTransactionStatus.Success;
            existingTransaction.Channel = channel ?? existingTransaction.Channel;
            existingTransaction.RawPayloadJson = rawPayload;
            existingTransaction.ProcessedAt = DateTime.UtcNow;
            existingTransaction.Updated(createdBy);
            await orderRepository.UpdatePaymentTransactionAsync(existingTransaction, cancellationToken);
        }

        await confirmInvestmentOrderService.TryFulfillAsync(order, createdBy, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> TryMarkFailedChargeAsync(
        string reference,
        string rawPayload,
        CancellationToken cancellationToken = default)
    {
        var order = await orderRepository.GetByPaystackReferenceAsync(reference, cancellationToken);
        if (order == null)
        {
            return false;
        }

        if (order.Status == InvestmentOrderStatus.Paid)
        {
            return true;
        }

        var actor = ResolveActor();
        order.Status = InvestmentOrderStatus.Failed;
        order.Updated(actor);
        await orderRepository.UpdateAsync(order, cancellationToken);

        var existingTransaction = await orderRepository.GetPaymentTransactionByReferenceAsync(reference, cancellationToken);
        if (existingTransaction == null)
        {
            var transaction = new PaymentTransaction
            {
                OrderId = order.Id,
                Reference = reference,
                Status = PaymentTransactionStatus.Failed,
                RawPayloadJson = rawPayload,
                ProcessedAt = DateTime.UtcNow,
            };
            transaction.Created(actor);
            await orderRepository.AddPaymentTransactionAsync(transaction, cancellationToken);
        }
        else
        {
            existingTransaction.Status = PaymentTransactionStatus.Failed;
            existingTransaction.RawPayloadJson = rawPayload;
            existingTransaction.ProcessedAt = DateTime.UtcNow;
            existingTransaction.Updated(actor);
            await orderRepository.UpdatePaymentTransactionAsync(existingTransaction, cancellationToken);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return true;
    }

    private string ResolveActor() =>
        !string.IsNullOrEmpty(currentUser.UserName)
            ? currentUser.UserName
            : (!string.IsNullOrEmpty(currentUser.IPAddress) ? currentUser.IPAddress : "PaystackWebhook");
}
