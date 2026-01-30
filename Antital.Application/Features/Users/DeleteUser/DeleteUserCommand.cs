using BuildingBlocks.Application.Features;

namespace Antital.Application.Features.Users.DeleteUser;

public record DeleteUserCommand(int Id) : ICommandQuery;
