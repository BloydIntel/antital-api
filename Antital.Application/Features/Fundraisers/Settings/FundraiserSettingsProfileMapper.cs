using Antital.Application.DTOs.Fundraisers;
using Antital.Domain.Models;

namespace Antital.Application.Features.Fundraisers.Settings;

internal static class FundraiserSettingsProfileMapper
{
    public static FundraiserSettingsProfileResponse ToResponse(UserInvestmentProfile? profile)
    {
        var companyName = NullIfWhiteSpace(profile?.CompanyLegalName)
            ?? NullIfWhiteSpace(profile?.TradingBrandName);
        var headquarters = NullIfWhiteSpace(profile?.BusinessAddress)
            ?? NullIfWhiteSpace(profile?.RegisteredAddress);

        return new FundraiserSettingsProfileResponse(
            CompanyName: companyName,
            RegistrationNumber: NullIfWhiteSpace(profile?.RegistrationNumber),
            Bio: NullIfWhiteSpace(profile?.BusinessDescription),
            Website: NullIfWhiteSpace(profile?.CompanyWebsite),
            PublicEmail: NullIfWhiteSpace(profile?.CompanyEmail),
            Headquarters: headquarters,
            LocationLabel: DeriveLocationLabel(headquarters),
            CompanyAvatarUrl: null,
            CompanyAvatarFallback: DeriveAvatarFallback(companyName),
            CompletionPercentage: ComputeCompletionPercentage(profile),
            Contact: new FundraiserSettingsContactDto(
                FullName: NullIfWhiteSpace(profile?.RepresentativeFullName),
                EmailAddress: NullIfWhiteSpace(profile?.RepresentativeEmail),
                PhoneNumber: NullIfWhiteSpace(profile?.RepresentativePhoneNumber),
                IsWhatsAppConnected: false,
                HasPublicHelpDesk: false));
    }

    public static void ApplyUpdate(UserInvestmentProfile profile, UpdateFundraiserSettingsProfileRequest request)
    {
        profile.CompanyLegalName = request.CompanyName.Trim();
        profile.RegistrationNumber = NullIfWhiteSpace(request.RegistrationNumber);
        profile.BusinessDescription = NullIfWhiteSpace(request.Bio);
        profile.CompanyWebsite = NullIfWhiteSpace(request.Website);
        profile.CompanyEmail = NullIfWhiteSpace(request.PublicEmail);
        profile.BusinessAddress = NullIfWhiteSpace(request.Headquarters);

        if (request.Contact is null)
        {
            return;
        }

        profile.RepresentativeFullName = NullIfWhiteSpace(request.Contact.FullName);
        profile.RepresentativeEmail = NullIfWhiteSpace(request.Contact.EmailAddress);
        profile.RepresentativePhoneNumber = NullIfWhiteSpace(request.Contact.PhoneNumber);
    }

    private static int ComputeCompletionPercentage(UserInvestmentProfile? profile)
    {
        if (profile is null)
        {
            return 0;
        }

        var filled = 0;
        const int total = 9;

        if (!string.IsNullOrWhiteSpace(profile.CompanyLegalName) || !string.IsNullOrWhiteSpace(profile.TradingBrandName))
            filled++;
        if (!string.IsNullOrWhiteSpace(profile.RegistrationNumber))
            filled++;
        if (!string.IsNullOrWhiteSpace(profile.BusinessDescription))
            filled++;
        if (!string.IsNullOrWhiteSpace(profile.CompanyWebsite))
            filled++;
        if (!string.IsNullOrWhiteSpace(profile.CompanyEmail))
            filled++;
        if (!string.IsNullOrWhiteSpace(profile.BusinessAddress) || !string.IsNullOrWhiteSpace(profile.RegisteredAddress))
            filled++;
        if (!string.IsNullOrWhiteSpace(profile.RepresentativeFullName))
            filled++;
        if (!string.IsNullOrWhiteSpace(profile.RepresentativeEmail))
            filled++;
        if (!string.IsNullOrWhiteSpace(profile.RepresentativePhoneNumber))
            filled++;

        return (int)Math.Round(filled * 100.0 / total);
    }

    private static string? DeriveLocationLabel(string? headquarters)
    {
        if (string.IsNullOrWhiteSpace(headquarters))
        {
            return null;
        }

        var parts = headquarters
            .Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

        if (parts.Length >= 2)
        {
            return $"{parts[^2]}, {parts[^1]}";
        }

        return parts[0];
    }

    private static string? DeriveAvatarFallback(string? companyName)
    {
        if (string.IsNullOrWhiteSpace(companyName))
        {
            return null;
        }

        var words = companyName
            .Split(' ', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

        if (words.Length >= 2)
        {
            return $"{char.ToUpperInvariant(words[0][0])}{char.ToUpperInvariant(words[1][0])}";
        }

        var single = words[0];
        return single.Length >= 2
            ? single[..2].ToUpperInvariant()
            : single.ToUpperInvariant();
    }

    private static string? NullIfWhiteSpace(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
