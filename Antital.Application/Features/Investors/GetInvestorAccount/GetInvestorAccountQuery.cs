using Antital.Application.DTOs.Investors;
using Antital.Application.Features.Investors.Account;
using Antital.Domain.Interfaces;
using BuildingBlocks.Application.Features;

namespace Antital.Application.Features.Investors.GetInvestorAccount;

public record GetInvestorAccountQuery : ICommandQuery<InvestorAccountResponse>;

public class GetInvestorAccountQueryHandler(
    IInvestorUserAccess investorUserAccess,
    IUserOnboardingRepository userOnboardingRepository,
    IUserInvestmentProfileRepository userInvestmentProfileRepository,
    IUserKycRepository userKycRepository
) : ICommandQueryHandler<GetInvestorAccountQuery, InvestorAccountResponse>
{
    public async Task<Result<InvestorAccountResponse>> Handle(
        GetInvestorAccountQuery request,
        CancellationToken cancellationToken)
    {
        var (userId, user) = await investorUserAccess.RequireAuthenticatedUserAsync(cancellationToken);

        var onboarding = await userOnboardingRepository.GetByUserIdAsync(userId, cancellationToken);
        var profile = await userInvestmentProfileRepository.GetByUserIdAsync(userId, cancellationToken);
        var kyc = await userKycRepository.GetByUserIdAsync(userId, cancellationToken);

        var response = InvestorAccountMapper.ToResponse(user, onboarding, profile, kyc);
        var result = new Result<InvestorAccountResponse>();
        result.AddValue(response);
        result.OK();
        return result;
    }
}
