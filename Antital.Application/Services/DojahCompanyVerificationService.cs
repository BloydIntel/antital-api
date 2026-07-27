using System.Globalization;
using System.Text.RegularExpressions;
using Antital.Domain.Configuration;
using Antital.Domain.Interfaces;
using BuildingBlocks.Application.Exceptions;
using Microsoft.Extensions.Options;

namespace Antital.Application.Services;

public sealed class DojahCompanyVerificationService(
    IDojahClient dojahClient,
    IOptions<DojahSettings> dojahOptions
) : ICompanyVerificationService
{
    public Task<CompanyVerificationResult> VerifyCorporateCompanyAsync(
        CorporateCompanyVerificationInput input,
        CancellationToken cancellationToken = default) =>
        VerifyAsync(
            input.CompanyLegalName,
            input.RegistrationType,
            input.RegistrationNumber,
            input.DateOfRegistration,
            cancellationToken);

    public Task<CompanyVerificationResult> VerifyFundraiserCompanyAsync(
        FundraiserCompanyVerificationInput input,
        CancellationToken cancellationToken = default) =>
        VerifyAsync(
            input.CompanyLegalName,
            input.RegistrationType,
            input.RegistrationNumber,
            input.DateOfRegistration,
            cancellationToken);

    private async Task<CompanyVerificationResult> VerifyAsync(
        string companyLegalName,
        string registrationType,
        string registrationNumber,
        DateTime? dateOfRegistration,
        CancellationToken cancellationToken)
    {
        if (!dojahOptions.Value.Enabled)
        {
            return new CompanyVerificationResult(
                null,
                registrationNumber,
                MapCompanyType(registrationType),
                "Bypassed",
                null,
                dateOfRegistration);
        }

        var companyType = MapCompanyType(registrationType);
        var lookup = await dojahClient.LookupCacAsync(registrationNumber.Trim(), companyType, cancellationToken);
        if (!lookup.IsSuccess)
        {
            throw new BadRequestException(
                "Company verification failed.",
                new Dictionary<string, string[]>
                {
                    ["registrationNumber"] = [lookup.ErrorMessage ?? "Unable to verify company registration with CAC."],
                });
        }

        if (!string.IsNullOrWhiteSpace(lookup.CompanyName)
            && !CompanyNamesMatch(companyLegalName, lookup.CompanyName))
        {
            throw new BadRequestException(
                "Company verification failed.",
                new Dictionary<string, string[]>
                {
                    ["companyLegalName"] = ["CAC company name does not match the provided legal name."],
                });
        }

        if (!string.IsNullOrWhiteSpace(lookup.Status)
            && !IsAcceptableStatus(lookup.Status))
        {
            throw new BadRequestException(
                "Company verification failed.",
                new Dictionary<string, string[]>
                {
                    ["registrationNumber"] = [$"CAC company status is {lookup.Status}, not active."],
                });
        }

        if (dateOfRegistration.HasValue
            && TryParseCacDate(lookup.IncorporationDate, out var providerDate)
            && providerDate.Date != dateOfRegistration.Value.Date)
        {
            throw new BadRequestException(
                "Company verification failed.",
                new Dictionary<string, string[]>
                {
                    ["dateOfRegistration"] = ["CAC registration date does not match the provided company registration date."],
                });
        }

        return new CompanyVerificationResult(
            lookup.CompanyName,
            lookup.RegistrationNumber ?? registrationNumber.Trim(),
            lookup.CompanyType ?? companyType,
            lookup.Status ?? "Verified",
            DateTime.UtcNow,
            TryParseCacDate(lookup.IncorporationDate, out var incorporationDate)
                ? incorporationDate.Date
                : dateOfRegistration?.Date);
    }

    private static bool TryParseCacDate(string? value, out DateTime date)
    {
        date = default;
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        return DateTime.TryParse(
            value.Trim(),
            CultureInfo.InvariantCulture,
            DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
            out date);
    }

    private static bool IsAcceptableStatus(string status)
    {
        var normalized = NormalizeTokens(status);
        return normalized is "ACTIVE" or "REGISTERED" or "INCORPORATED";
    }

    private static string MapCompanyType(string registrationType)
    {
        var normalized = registrationType.Trim().ToUpperInvariant();
        if (normalized.Contains("BN"))
        {
            return "BUSINESS_NAME";
        }

        return "COMPANY";
    }

    private static bool CompanyNamesMatch(string left, string right)
    {
        var leftTokens = Tokenize(left);
        var rightTokens = Tokenize(right);

        return leftTokens.SequenceEqual(rightTokens);
    }

    private static IReadOnlyList<string> Tokenize(string value) =>
        NormalizeTokens(value)
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(token => !IgnoredCompanyTokens.Contains(token))
            .ToArray();

    private static string NormalizeTokens(string value)
    {
        var normalized = Regex.Replace(value.Trim().ToUpperInvariant(), @"[^A-Z0-9]+", " ");
        return Regex.Replace(normalized, @"\s+", " ").Trim();
    }

    private static readonly HashSet<string> IgnoredCompanyTokens =
    [
        "LIMITED",
        "LTD",
        "PLC",
        "LLC",
        "INC",
        "INCORPORATED",
        "COMPANY",
        "CO",
        "BUSINESS",
        "NAME",
    ];
}
