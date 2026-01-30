using BuildingBlocks.Application.Features;

namespace Antital.Application.Features.Authentication.ResendVerificationEmail;

public record ResendVerificationEmailCommand(string Email) : ICommandQuery;
