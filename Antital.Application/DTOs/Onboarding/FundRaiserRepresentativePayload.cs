namespace Antital.Application.DTOs.Onboarding;

public record FundRaiserRepresentativePayload(
    string RepresentativeFullName,
    string RepresentativeJobTitle,
    string RepresentativePhoneNumber,
    DateTime RepresentativeDateOfBirth,
    string RepresentativeEmail,
    string RepresentativeNationality,
    string RepresentativeCountryOfResidence,
    string RepresentativeAddress
);
