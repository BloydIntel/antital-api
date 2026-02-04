using BuildingBlocks.Domain.Interfaces;

namespace Antital.Domain.Interfaces;

public interface IAntitalUnitOfWork : IUnitOfWork
{
    IUserRepository UserRepository { get; init; }
    IUserOnboardingRepository UserOnboardingRepository { get; init; }
    IUserInvestmentProfileRepository UserInvestmentProfileRepository { get; init; }
    IUserKycRepository UserKycRepository { get; init; }
}
