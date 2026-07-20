using Antital.Domain.Enums;

namespace Antital.Application.DTOs;

public class UserDto
{
    public int Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public string? PreferredName { get; set; }
    public UserTypeEnum UserType { get; set; }
    public bool IsEmailVerified { get; set; }

    /// <summary>Personal details captured at signup (individual / representative-linked user row).</summary>
    public DateTime? DateOfBirth { get; set; }
    public string? Nationality { get; set; }
    public string? CountryOfResidence { get; set; }
    public string? StateOfResidence { get; set; }
    public string? ResidentialAddress { get; set; }
    public bool HasAgreedToTerms { get; set; }

    /// <summary>Corporate / fundraiser company details from investment profile when present.</summary>
    public UserCompanyDto? Company { get; set; }
}

public class UserCompanyDto
{
    public string? CompanyLegalName { get; set; }
    public string? TradingBrandName { get; set; }
    public string? RegistrationType { get; set; }
    public string? RegistrationNumber { get; set; }
    public string? CompanyLoginEmail { get; set; }
    public DateTime? DateOfRegistration { get; set; }
    public string? CompanyWebsite { get; set; }
    public string? BusinessAddress { get; set; }
    public string? RegisteredAddress { get; set; }
    public string? CompanyEmail { get; set; }
    public string? CompanyPhone { get; set; }
    public string? RepresentativeFullName { get; set; }
    public string? RepresentativeJobTitle { get; set; }
    public string? RepresentativePhoneNumber { get; set; }
    public DateTime? RepresentativeDateOfBirth { get; set; }
    public string? RepresentativeEmail { get; set; }
    public string? RepresentativeNationality { get; set; }
    public string? RepresentativeCountryOfResidence { get; set; }
    public string? RepresentativeAddress { get; set; }
}
