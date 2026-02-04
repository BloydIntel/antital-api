using Antital.Domain.Models;

namespace Antital.Domain.Interfaces;

public interface IUserKycRepository
{
    Task<UserKyc?> GetByUserIdAsync(int userId, CancellationToken cancellationToken);
    Task AddAsync(UserKyc entity, CancellationToken cancellationToken);
    Task UpdateAsync(UserKyc entity, CancellationToken cancellationToken);
}
