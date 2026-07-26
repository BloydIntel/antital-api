using Antital.Domain.Configuration;
using Antital.Domain.Enums;
using Antital.Domain.Interfaces;
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

        var idLookup = await LookupGovernmentIdAsync(idType, idNumber!, user.LastName, cancellationToken);
        if (!idLookup.IsSuccess)
        {
            throw new BadRequestException(
                "Identity verification failed.",
                new Dictionary<string, string[]>
                {
                    ["nin"] = [idLookup.ErrorMessage ?? "Unable to verify government ID."],
                });
        }

        if (!KycIdentityMatcher.NamesMatch(user.FirstName, user.LastName, idLookup.FirstName, idLookup.LastName)
            || !KycIdentityMatcher.DatesOfBirthMatch(user.DateOfBirth, idLookup.DateOfBirth))
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
        if (!KycIdentityMatcher.NamesMatch(user.FirstName, user.LastName, bvnLookup.FirstName, bvnLookup.LastName))
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
}
