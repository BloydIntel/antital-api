using Antital.Domain.Configuration;
using Antital.Domain.Enums;
using Antital.Domain.Interfaces;
using Antital.Domain.Models;
using BuildingBlocks.Application.Exceptions;
using Microsoft.Extensions.Options;

namespace Antital.Application.Services;

/// <summary>
/// Verifies government ID + BVN via Dojah when <see cref="DojahSettings.Enabled"/> is true.
/// Falls back to pass-through (no VerifiedAt) when disabled.
/// </summary>
public sealed class DojahKycVerificationService(
    IDojahClient dojahClient,
    IUserRepository userRepository,
    IUserInvestmentProfileRepository userInvestmentProfileRepository,
    IOptions<DojahSettings> dojahOptions,
    PassThroughKycVerificationService passThrough
) : IKycVerificationService
{
    public async Task<KycVerificationResult> ProcessAsync(
        KycVerificationInput input,
        CancellationToken cancellationToken = default)
    {
        if (!dojahOptions.Value.Enabled)
        {
            return await passThrough.ProcessAsync(input, cancellationToken);
        }

        var user = await userRepository.GetByIdAsync(input.UserId, cancellationToken)
            ?? throw new BadRequestException(
                "Unable to verify identity for this user.",
                new Dictionary<string, string[]>());
        var profile = user.UserType == UserTypeEnum.IndividualInvestor
            ? null
            : await userInvestmentProfileRepository.GetByUserIdAsync(input.UserId, cancellationToken);
        var comparisonIdentity = ResolveComparisonIdentity(user, profile);

        var idType = (KycIdType)input.IdType;
        var idNumber = input.Nin?.Trim();
        var bvn = input.Bvn?.Trim();

        var errors = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);

        if (string.IsNullOrWhiteSpace(idNumber))
        {
            errors["nin"] = ["ID number is required."];
        }

        if (string.IsNullOrWhiteSpace(bvn))
        {
            errors["bvn"] = ["BVN is required."];
        }

        if (errors.Count > 0)
        {
            throw new BadRequestException("Identity verification failed.", errors);
        }

        var idLookup = await LookupGovernmentIdAsync(idType, idNumber!, comparisonIdentity.LastName, cancellationToken);
        if (!idLookup.IsSuccess)
        {
            throw new BadRequestException(
                "Identity verification failed.",
                new Dictionary<string, string[]>
                {
                    ["nin"] = [idLookup.ErrorMessage ?? "Unable to verify government ID."],
                });
        }

        if (!KycIdentityMatcher.NamesMatch(comparisonIdentity.FirstName, comparisonIdentity.LastName, idLookup.FirstName, idLookup.LastName)
            || !KycIdentityMatcher.DatesOfBirthMatch(comparisonIdentity.DateOfBirth, idLookup.DateOfBirth))
        {
            throw new BadRequestException(
                "Identity verification failed.",
                new Dictionary<string, string[]>
                {
                    ["nin"] =
                    [
                        "Government ID details do not match your profile name or date of birth.",
                    ],
                });
        }

        var bvnLookup = await dojahClient.LookupBvnAsync(bvn!, cancellationToken);
        if (!bvnLookup.IsSuccess)
        {
            throw new BadRequestException(
                "Identity verification failed.",
                new Dictionary<string, string[]>
                {
                    ["bvn"] = [bvnLookup.ErrorMessage ?? "Unable to verify BVN."],
                });
        }

        // Name-only for BVN: Dojah sandbox (and some live records) can return a different DOB
        // than NIN/passport for the same person. Government ID remains the DOB source of truth.
        if (!KycIdentityMatcher.NamesMatch(comparisonIdentity.FirstName, comparisonIdentity.LastName, bvnLookup.FirstName, bvnLookup.LastName))
        {
            throw new BadRequestException(
                "Identity verification failed.",
                new Dictionary<string, string[]>
                {
                    ["bvn"] = ["BVN details do not match your profile name."],
                });
        }

        var now = DateTime.UtcNow;
        return new KycVerificationResult(
            input.GovernmentIdDocumentPathOrKey,
            input.ProofOfAddressDocumentPathOrKey,
            input.SelfieVerificationPathOrKey,
            input.IncomeVerificationPathOrKey,
            GovernmentIdVerifiedAt: now,
            ProofOfAddressVerifiedAt: null,
            SelfieVerifiedAt: null,
            IncomeVerifiedAt: null
        );
    }

    private static ComparisonIdentity ResolveComparisonIdentity(User user, UserInvestmentProfile? profile)
    {
        if ((user.UserType == UserTypeEnum.FundRaiser || user.UserType == UserTypeEnum.CorporateInvestor)
            && TryParseRepresentativeIdentity(profile, out var representativeIdentity))
        {
            return representativeIdentity;
        }

        return new ComparisonIdentity(user.FirstName, user.LastName, user.DateOfBirth);
    }

    private static bool TryParseRepresentativeIdentity(UserInvestmentProfile? profile, out ComparisonIdentity identity)
    {
        identity = default;

        if (profile?.RepresentativeDateOfBirth == null || string.IsNullOrWhiteSpace(profile.RepresentativeFullName))
        {
            return false;
        }

        var tokens = profile.RepresentativeFullName
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (tokens.Length < 2)
        {
            return false;
        }

        identity = new ComparisonIdentity(tokens[0], tokens[^1], profile.RepresentativeDateOfBirth.Value);
        return true;
    }

    private Task<DojahIdentityLookupResult> LookupGovernmentIdAsync(
        KycIdType idType,
        string idNumber,
        string surname,
        CancellationToken cancellationToken) =>
        idType switch
        {
            KycIdType.NationalIdCard => dojahClient.LookupNinAsync(idNumber, cancellationToken),
            KycIdType.InternationalPassport => dojahClient.LookupPassportAsync(idNumber, surname, cancellationToken),
            KycIdType.DriversLicence => dojahClient.LookupDriversLicenceAsync(idNumber, cancellationToken),
            _ => Task.FromResult(
                DojahIdentityLookupResult.Fail(400, null, "Unsupported ID type.")),
        };

    private readonly record struct ComparisonIdentity(string FirstName, string LastName, DateTime DateOfBirth);
}
