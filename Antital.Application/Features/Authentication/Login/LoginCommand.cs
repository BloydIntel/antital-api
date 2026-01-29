using Antital.Application.DTOs.Authentication;
using BuildingBlocks.Application.Features;

namespace Antital.Application.Features.Authentication.Login;

public record LoginCommand(
    string Email,
    string Password
) : ICommandQuery<AuthResponseDto>;
