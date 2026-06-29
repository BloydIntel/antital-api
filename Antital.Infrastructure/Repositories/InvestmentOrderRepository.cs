using Antital.Domain.Enums;
using Antital.Domain.Interfaces;
using Antital.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace Antital.Infrastructure.Repositories;

public class InvestmentOrderRepository(AntitalDBContext context) : IInvestmentOrderRepository
{
    public Task<InvestmentOrder?> GetByIdAsync(int orderId, CancellationToken cancellationToken = default) =>
        context.InvestmentOrders
            .Include(o => o.Offering)
            .ThenInclude(offering => offering.Funding)
            .FirstOrDefaultAsync(o => o.Id == orderId && !o.IsDeleted, cancellationToken);

    public Task<InvestmentOrder?> GetByIdForUserAsync(int orderId, int userId, CancellationToken cancellationToken = default) =>
        context.InvestmentOrders
            .Include(o => o.Offering)
            .ThenInclude(offering => offering.Funding)
            .FirstOrDefaultAsync(o => o.Id == orderId && o.UserId == userId && !o.IsDeleted, cancellationToken);

    public Task<InvestmentOrder?> GetByPaystackReferenceAsync(string reference, CancellationToken cancellationToken = default) =>
        context.InvestmentOrders
            .Include(o => o.Offering)
            .ThenInclude(offering => offering.Funding)
            .FirstOrDefaultAsync(o => o.PaystackReference == reference && !o.IsDeleted, cancellationToken);

    public Task<InvestmentOrder?> GetPendingByUserAndOfferingAsync(
        int userId,
        int offeringId,
        CancellationToken cancellationToken = default) =>
        context.InvestmentOrders
            .FirstOrDefaultAsync(
                o => o.UserId == userId
                     && o.OfferingId == offeringId
                     && o.Status == InvestmentOrderStatus.PendingPayment
                     && !o.IsDeleted,
                cancellationToken);

    public async Task AddAsync(InvestmentOrder order, CancellationToken cancellationToken = default)
    {
        await context.InvestmentOrders.AddAsync(order, cancellationToken);
    }

    public async Task AddPaymentTransactionAsync(PaymentTransaction transaction, CancellationToken cancellationToken = default)
    {
        await context.PaymentTransactions.AddAsync(transaction, cancellationToken);
    }

    public Task<PaymentTransaction?> GetPaymentTransactionByReferenceAsync(
        string reference,
        CancellationToken cancellationToken = default) =>
        context.PaymentTransactions
            .FirstOrDefaultAsync(t => t.Reference == reference && !t.IsDeleted, cancellationToken);

    public Task UpdatePaymentTransactionAsync(PaymentTransaction transaction, CancellationToken cancellationToken = default)
    {
        context.PaymentTransactions.Update(transaction);
        return Task.CompletedTask;
    }

    public Task<InvestorHolding?> GetHoldingByUserAndOfferingAsync(
        int userId,
        int offeringId,
        CancellationToken cancellationToken = default) =>
        context.InvestorHoldings
            .FirstOrDefaultAsync(
                h => h.UserId == userId && h.OfferingId == offeringId && !h.IsDeleted,
                cancellationToken);

    public async Task AddInvestorHoldingAsync(InvestorHolding holding, CancellationToken cancellationToken = default)
    {
        await context.InvestorHoldings.AddAsync(holding, cancellationToken);
    }

    public Task UpdateInvestorHoldingAsync(InvestorHolding holding, CancellationToken cancellationToken = default)
    {
        context.InvestorHoldings.Update(holding);
        return Task.CompletedTask;
    }

    public Task<OfferingFunding?> GetOfferingFundingForUpdateAsync(
        int offeringId,
        CancellationToken cancellationToken = default) =>
        context.OfferingFundings
            .FirstOrDefaultAsync(f => f.OfferingId == offeringId && !f.IsDeleted, cancellationToken);

    public Task UpdateOfferingFundingAsync(OfferingFunding funding, CancellationToken cancellationToken = default)
    {
        context.OfferingFundings.Update(funding);
        return Task.CompletedTask;
    }

    public Task UpdateAsync(InvestmentOrder order, CancellationToken cancellationToken = default)
    {
        context.InvestmentOrders.Update(order);
        return Task.CompletedTask;
    }

    public async Task<(IReadOnlyList<InvestmentOrder> Items, int TotalCount)> ListPaidByUserAsync(
        int userId,
        int page,
        int pageSize,
        DateTime? fromUtc,
        DateTime? toUtc,
        CancellationToken cancellationToken = default)
    {
        var query = context.InvestmentOrders
            .AsNoTracking()
            .Include(o => o.Offering)
            .Where(o => o.UserId == userId
                        && o.Status == InvestmentOrderStatus.Paid
                        && !o.IsDeleted);

        if (fromUtc.HasValue)
        {
            query = query.Where(o => (o.PaidAt ?? o.UpdatedAt ?? o.CreatedAt) >= fromUtc.Value);
        }

        if (toUtc.HasValue)
        {
            query = query.Where(o => (o.PaidAt ?? o.UpdatedAt ?? o.CreatedAt) <= toUtc.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(o => o.PaidAt ?? o.UpdatedAt ?? o.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }
}
