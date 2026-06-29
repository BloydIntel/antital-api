using Antital.Domain.Models;

namespace Antital.Domain.Interfaces;

public interface IInvestorPaymentMethodRepository
{
    Task<IReadOnlyList<InvestorPaymentMethod>> ListByUserAsync(int userId, CancellationToken cancellationToken = default);

    Task<InvestorPaymentMethod?> GetByIdForUserAsync(int id, int userId, CancellationToken cancellationToken = default);

    Task<InvestorPaymentMethod?> GetDefaultForUserAsync(int userId, CancellationToken cancellationToken = default);

    Task AddAsync(InvestorPaymentMethod method, CancellationToken cancellationToken = default);

    Task ClearDefaultForUserAsync(int userId, CancellationToken cancellationToken = default);

    Task UpdateAsync(InvestorPaymentMethod method, CancellationToken cancellationToken = default);

    Task PromoteNextDefaultAsync(int userId, int excludedId, string actor, CancellationToken cancellationToken = default);
}
