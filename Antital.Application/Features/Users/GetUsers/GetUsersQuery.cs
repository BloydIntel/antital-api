using Antital.Application.DTOs;
using BuildingBlocks.Application.Features;

namespace Antital.Application.Features.Users.GetUsers;

public record GetUsersQuery() : ICommandQuery<List<UserDto>>;
