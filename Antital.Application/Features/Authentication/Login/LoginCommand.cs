using BuildingBlocks.Application.Features;

namespace Antital.Application.Features.Authentication.Login;

public record LoginCommand(
    string UserName,
    string Password
    ) : ICommandQuery<string>;
