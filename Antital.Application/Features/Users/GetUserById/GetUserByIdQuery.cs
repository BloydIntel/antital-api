using Antital.Application.DTOs;
using BuildingBlocks.Application.Features;

namespace Antital.Application.Features.Users.GetUserById;

public record GetUserByIdQuery(int Id) : ICommandQuery<UserDto>;
