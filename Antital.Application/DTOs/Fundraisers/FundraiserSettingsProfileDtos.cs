namespace Antital.Application.DTOs.Fundraisers;

public record FundraiserSettingsContactDto(
    string? FullName,
    string? EmailAddress,
    string? PhoneNumber,
    bool IsWhatsAppConnected,
    bool HasPublicHelpDesk);

public record FundraiserSettingsProfileResponse(
    string? CompanyName,
    string? RegistrationNumber,
    string? Bio,
    string? Website,
    string? PublicEmail,
    string? Headquarters,
    string? LocationLabel,
    string? CompanyAvatarUrl,
    string? CompanyAvatarFallback,
    int CompletionPercentage,
    FundraiserSettingsContactDto Contact);

public record UpdateFundraiserSettingsContactRequest(
    string? FullName,
    string? EmailAddress,
    string? PhoneNumber);

public record UpdateFundraiserSettingsProfileRequest(
    string CompanyName,
    string? RegistrationNumber,
    string? Bio,
    string? Website,
    string? PublicEmail,
    string? Headquarters,
    UpdateFundraiserSettingsContactRequest? Contact);
