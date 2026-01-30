using Antital.Application.DTOs;
using Antital.Domain.Enums;
using BuildingBlocks.Application.Features;

namespace Antital.Application.Features.Users.CreateUser;

public record CreateUserCommand(
    string Email,
    string Password,
    string FirstName,
    string LastName,
    string? PreferredName,
    string? PhoneNumber,
    UserTypeEnum UserType
) : ICommandQuery<UserDto>;
