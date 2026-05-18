using Antital.Domain.Enums;
using Antital.Domain.Interfaces;
using Antital.Domain.Models;
using BuildingBlocks.Application.Exceptions;
using BuildingBlocks.Application.Features;
using Microsoft.Extensions.Logging;

namespace Antital.Application.Features.Onboarding.SubmitOnboarding;

public class SubmitOnboardingCommandHandler(
    IOnboardingUserAccess userAccess,
    IAntitalUnitOfWork unitOfWork,
    IUserOnboardingRepository userOnboardingRepository,
    IUserInvestmentProfileRepository userInvestmentProfileRepository,
    IUserKycRepository userKycRepository,
    IEmailService emailService,
    ILogger<SubmitOnboardingCommandHandler> logger
) : ICommandQueryHandler<SubmitOnboardingCommand>
{
    public async Task<Result> Handle(SubmitOnboardingCommand request, CancellationToken cancellationToken)
    {
        var (userId, user) = await userAccess.RequireVerifiedUserAsync(cancellationToken);

        var onboarding = await userOnboardingRepository.GetByUserIdAsync(userId, cancellationToken);
        if (onboarding == null)
            throw new BadRequestException("Onboarding not started. Complete at least the investor category step.", new Dictionary<string, string[]>());

        if (onboarding.Status == OnboardingStatus.Submitted)
            throw new BadRequestException("Onboarding has already been submitted.", new Dictionary<string, string[]>());

        // Minimum completeness for individual: at least investor category (and ideally profile)
        var profile = await userInvestmentProfileRepository.GetByUserIdAsync(userId, cancellationToken);
        if (profile == null)
            throw new BadRequestException("Complete the investor category and investment profile before submitting.", new Dictionary<string, string[]>());

        if (user.UserType == UserTypeEnum.FundRaiser)
        {
            EnsureFundRaiserSubmissionIsComplete(profile, await userKycRepository.GetByUserIdAsync(userId, cancellationToken));
        }

        onboarding.SubmittedAt = DateTime.UtcNow;
        onboarding.Status = OnboardingStatus.Submitted;
        onboarding.CurrentStep = OnboardingStep.Submitted;

        await userOnboardingRepository.UpdateAsync(onboarding, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        try
        {
            await emailService.SendOnboardingSubmittedEmailAsync(user.Email, user.UserType, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send onboarding completion email to {Email}.", user.Email);
        }

        var result = new Result();
        result.OK();
        return result;
    }

    private static void EnsureFundRaiserSubmissionIsComplete(UserInvestmentProfile profile, UserKyc? kyc)
    {
        var errors = new Dictionary<string, string[]>();

        if (string.IsNullOrWhiteSpace(profile.CompanyLegalName)
            || string.IsNullOrWhiteSpace(profile.RegistrationType)
            || string.IsNullOrWhiteSpace(profile.RegistrationNumber)
            || string.IsNullOrWhiteSpace(profile.CompanyLoginEmail)
            || profile.DateOfRegistration == null
            || string.IsNullOrWhiteSpace(profile.BusinessAddress)
            || string.IsNullOrWhiteSpace(profile.RegisteredAddress)
            || string.IsNullOrWhiteSpace(profile.CompanyEmail)
            || string.IsNullOrWhiteSpace(profile.CompanyPhone))
        {
            errors["Company"] = ["Fund raiser company information is incomplete."];
        }

        if (string.IsNullOrWhiteSpace(profile.RepresentativeFullName)
            || string.IsNullOrWhiteSpace(profile.RepresentativeJobTitle)
            || string.IsNullOrWhiteSpace(profile.RepresentativePhoneNumber)
            || profile.RepresentativeDateOfBirth == null
            || string.IsNullOrWhiteSpace(profile.RepresentativeEmail)
            || string.IsNullOrWhiteSpace(profile.RepresentativeNationality)
            || string.IsNullOrWhiteSpace(profile.RepresentativeCountryOfResidence)
            || string.IsNullOrWhiteSpace(profile.RepresentativeAddress))
        {
            errors["Representative"] = ["Fund raiser representative details are incomplete."];
        }

        if (string.IsNullOrWhiteSpace(profile.FounderAndTeamIntroductionDocumentPathOrKey)
            || string.IsNullOrWhiteSpace(profile.FundraisingDeckDocumentPathOrKey)
            || string.IsNullOrWhiteSpace(profile.InvestmentMemoDocumentPathOrKey)
            || string.IsNullOrWhiteSpace(profile.TermsOfOfferingDocumentPathOrKey)
            || string.IsNullOrWhiteSpace(profile.BusinessDescription)
            || string.IsNullOrWhiteSpace(profile.BusinessSector)
            || string.IsNullOrWhiteSpace(profile.InstrumentType)
            || string.IsNullOrWhiteSpace(profile.BusinessSize)
            || !profile.FundingTarget.HasValue
            || profile.FundingTarget <= 0
            || string.IsNullOrWhiteSpace(profile.InvestmentRound))
        {
            errors["BusinessDocuments"] = ["Fund raiser business documents and disclosures are incomplete."];
        }

        if (kyc == null
            || string.IsNullOrWhiteSpace(kyc.GovernmentIdDocumentPathOrKey)
            || string.IsNullOrWhiteSpace(kyc.ProofOfAddressDocumentPathOrKey))
        {
            errors["Kyc"] = ["Fund raiser representative KYC is incomplete."];
        }

        if (string.IsNullOrWhiteSpace(profile.FundRaiserPaymentMethod)
            || string.IsNullOrWhiteSpace(profile.FundRaiserPaymentReference)
            || string.IsNullOrWhiteSpace(profile.FundRaiserPaymentStatus)
            || profile.FundRaiserApplicationFeePaid != true)
        {
            errors["Payment"] = ["Fund raiser application fee is incomplete."];
        }

        if (errors.Count > 0)
        {
            throw new BadRequestException("Fund raiser onboarding is incomplete. Complete all required steps before submitting.", errors);
        }
    }
}
