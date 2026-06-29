using Antital.Domain.Interfaces;
using Antital.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace Antital.Infrastructure.Repositories;

public class InvestorPaymentMethodRepository(AntitalDBContext context) : IInvestorPaymentMethodRepository
{
    public async Task<IReadOnlyList<InvestorPaymentMethod>> ListByUserAsync(
        int userId,
        CancellationToken cancellationToken = default)
    {
        return await context.InvestorPaymentMethods
            .AsNoTracking()
            .Where(m => m.UserId == userId && !m.IsDeleted)
            .OrderByDescending(m => m.IsDefault)
            .ThenByDescending(m => m.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public Task<InvestorPaymentMethod?> GetByIdForUserAsync(
        int id,
        int userId,
        CancellationToken cancellationToken = default) =>
        context.InvestorPaymentMethods
            .FirstOrDefaultAsync(m => m.Id == id && m.UserId == userId && !m.IsDeleted, cancellationToken);

    public Task<InvestorPaymentMethod?> GetDefaultForUserAsync(
        int userId,
        CancellationToken cancellationToken = default) =>
        context.InvestorPaymentMethods
            .FirstOrDefaultAsync(m => m.UserId == userId && m.IsDefault && !m.IsDeleted, cancellationToken);

    public async Task AddAsync(InvestorPaymentMethod method, CancellationToken cancellationToken = default)
    {
        await context.InvestorPaymentMethods.AddAsync(method, cancellationToken);
    }

    public async Task ClearDefaultForUserAsync(int userId, CancellationToken cancellationToken = default)
    {
        var defaults = await context.InvestorPaymentMethods
            .Where(m => m.UserId == userId && m.IsDefault && !m.IsDeleted)
            .ToListAsync(cancellationToken);

        foreach (var method in defaults)
        {
            method.IsDefault = false;
            context.InvestorPaymentMethods.Update(method);
        }
    }

    public Task UpdateAsync(InvestorPaymentMethod method, CancellationToken cancellationToken = default)
    {
        context.InvestorPaymentMethods.Update(method);
        return Task.CompletedTask;
    }

    public async Task PromoteNextDefaultAsync(
        int userId,
        int excludedId,
        string actor,
        CancellationToken cancellationToken = default)
    {
        var next = await context.InvestorPaymentMethods
            .Where(m => m.UserId == userId && !m.IsDeleted && m.Id != excludedId)
            .OrderByDescending(m => m.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (next == null)
        {
            return;
        }

        next.IsDefault = true;
        next.Updated(actor);
        context.InvestorPaymentMethods.Update(next);
    }
}
