using BuildingBlocks.Application.Features;

namespace Antital.Application.Features.Authentication.Logout;

public record LogoutCommand(string RefreshToken) : ICommandQuery;
