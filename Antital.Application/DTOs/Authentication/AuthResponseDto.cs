using Antital.Domain.Enums;

namespace Antital.Application.DTOs.Authentication;

public class AuthResponseDto
{
    public string Token { get; set; } = string.Empty;
    public string? RefreshToken { get; set; }
    public int UserId { get; set; }
    public string Email { get; set; } = string.Empty;
    public UserTypeEnum UserType { get; set; }
    public bool IsEmailVerified { get; set; }
}