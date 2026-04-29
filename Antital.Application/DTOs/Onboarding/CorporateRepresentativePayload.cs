namespace Antital.Application.DTOs.Onboarding;

public record CorporateRepresentativePayload(
    string RepresentativeFullName,
    string RepresentativeJobTitle,
    string RepresentativePhoneNumber,
    DateTime RepresentativeDateOfBirth,
    string RepresentativeEmail,
    string RepresentativeNationality,
    string RepresentativeCountryOfResidence,
    string RepresentativeAddress
);
