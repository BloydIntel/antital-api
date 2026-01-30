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
}
