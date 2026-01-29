using BuildingBlocks.Application.Features;

namespace Antital.Application.Features.Authentication.ForgotPassword;

public record ForgotPasswordCommand(string Email) : ICommandQuery;
