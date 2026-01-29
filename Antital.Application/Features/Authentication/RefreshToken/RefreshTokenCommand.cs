using Antital.Application.DTOs.Authentication;
using BuildingBlocks.Application.Features;

namespace Antital.Application.Features.Authentication.RefreshToken;

public record RefreshTokenCommand(string RefreshToken) : ICommandQuery<AuthResponseDto>;
