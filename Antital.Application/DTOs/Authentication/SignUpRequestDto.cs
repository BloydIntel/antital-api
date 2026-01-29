namespace Antital.Application.DTOs.Authentication;

public class SignUpRequestDto
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? PreferredName { get; set; }
    public string PhoneNumber { get; set; } = string.Empty;
    public DateTime DateOfBirth { get; set; }
    public string Nationality { get; set; } = string.Empty;
    public string CountryOfResidence { get; set; } = string.Empty;
    public string StateOfResidence { get; set; } = string.Empty;
    public string ResidentialAddress { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string ConfirmPassword { get; set; } = string.Empty;
    public bool HasAgreedToTerms { get; set; }
}
