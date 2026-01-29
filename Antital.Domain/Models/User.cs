using BuildingBlocks.Domain.Models;
using Antital.Domain.Enums;

namespace Antital.Domain.Models;

public class User : TrackableEntity
{
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public UserTypeEnum UserType { get; set; }
    public bool IsEmailVerified { get; set; } = false;
    public string? EmailVerificationToken { get; set; }
    public DateTime? EmailVerificationTokenExpiry { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? PreferredName { get; set; }
    public string PhoneNumber { get; set; } = string.Empty;
    public DateTime DateOfBirth { get; set; }
    public string Nationality { get; set; } = string.Empty;
    public string CountryOfResidence { get; set; } = string.Empty;
    public string StateOfResidence { get; set; } = string.Empty;
    public string ResidentialAddress { get; set; } = string.Empty;
    public bool HasAgreedToTerms { get; set; }
}