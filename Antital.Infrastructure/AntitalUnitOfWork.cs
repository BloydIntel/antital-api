using BuildingBlocks.Infrastructure.Implementations;
using Antital.Domain.Interfaces;

namespace Antital.Infrastructure;

public class AntitalUnitOfWork(
    DBContext dbContext,
    IUserRepository userRepository,
    IUserOnboardingRepository userOnboardingRepository,
    IUserInvestmentProfileRepository userInvestmentProfileRepository,
    IUserKycRepository userKycRepository
) : UnitOfWork(dbContext), IAntitalUnitOfWork
{
    public IUserRepository UserRepository { get; init; } = userRepository;
    public IUserOnboardingRepository UserOnboardingRepository { get; init; } = userOnboardingRepository;
    public IUserInvestmentProfileRepository UserInvestmentProfileRepository { get; init; } = userInvestmentProfileRepository;
    public IUserKycRepository UserKycRepository { get; init; } = userKycRepository;
}
