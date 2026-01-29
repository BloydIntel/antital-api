using BuildingBlocks.Application.Features;

namespace Antital.Application.Features.Authentication.VerifyEmail;

public record VerifyEmailCommand(
    string Email,
    string Token
) : ICommandQuery;
