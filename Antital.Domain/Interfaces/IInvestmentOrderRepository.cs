using Antital.Domain.Enums;
using Antital.Domain.Models;

namespace Antital.Domain.Interfaces;

public interface IInvestmentOrderRepository
{
    Task<InvestmentOrder?> GetByIdAsync(int orderId, CancellationToken cancellationToken = default);

    Task<InvestmentOrder?> GetByIdForUserAsync(int orderId, int userId, CancellationToken cancellationToken = default);

    Task<InvestmentOrder?> GetByPaystackReferenceAsync(string reference, CancellationToken cancellationToken = default);

    Task<InvestmentOrder?> GetPendingByUserAndOfferingAsync(
        int userId,
        int offeringId,
        CancellationToken cancellationToken = default);

    Task AddAsync(InvestmentOrder order, CancellationToken cancellationToken = default);

    Task AddPaymentTransactionAsync(PaymentTransaction transaction, CancellationToken cancellationToken = default);

    Task<PaymentTransaction?> GetPaymentTransactionByReferenceAsync(
        string reference,
        CancellationToken cancellationToken = default);

    Task UpdatePaymentTransactionAsync(PaymentTransaction transaction, CancellationToken cancellationToken = default);

    Task<InvestorHolding?> GetHoldingByUserAndOfferingAsync(
        int userId,
        int offeringId,
        CancellationToken cancellationToken = default);

    Task AddInvestorHoldingAsync(InvestorHolding holding, CancellationToken cancellationToken = default);

    Task UpdateInvestorHoldingAsync(InvestorHolding holding, CancellationToken cancellationToken = default);

    Task<OfferingFunding?> GetOfferingFundingForUpdateAsync(
        int offeringId,
        CancellationToken cancellationToken = default);

    Task UpdateOfferingFundingAsync(OfferingFunding funding, CancellationToken cancellationToken = default);

    Task UpdateAsync(InvestmentOrder order, CancellationToken cancellationToken = default);
}
