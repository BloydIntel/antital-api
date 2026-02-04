using Antital.Domain.Models;

namespace Antital.Domain.Interfaces;

public interface IUserInvestmentProfileRepository
{
    Task<UserInvestmentProfile?> GetByUserIdAsync(int userId, CancellationToken cancellationToken);
    Task AddAsync(UserInvestmentProfile entity, CancellationToken cancellationToken);
    Task UpdateAsync(UserInvestmentProfile entity, CancellationToken cancellationToken);
}
