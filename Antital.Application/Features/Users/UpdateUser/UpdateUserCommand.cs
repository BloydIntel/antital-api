using Antital.Domain.Enums;
using BuildingBlocks.Application.Features;

namespace Antital.Application.Features.Users.UpdateUser;

public record UpdateUserCommand(
    int Id,
    string FirstName,
    string LastName,
    string? PreferredName,
    string? PhoneNumber,
    UserTypeEnum UserType,
    bool? IsEmailVerified,
    string? Password // optional; if null/empty, keep existing
) : ICommandQuery;
