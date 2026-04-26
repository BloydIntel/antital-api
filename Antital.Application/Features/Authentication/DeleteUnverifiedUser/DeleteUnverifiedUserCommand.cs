using BuildingBlocks.Application.Features;

namespace Antital.Application.Features.Authentication.DeleteUnverifiedUser;

public record DeleteUnverifiedUserCommand(
    string Email,
    string Token
) : ICommandQuery;
