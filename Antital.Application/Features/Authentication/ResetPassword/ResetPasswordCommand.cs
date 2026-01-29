using BuildingBlocks.Application.Features;

namespace Antital.Application.Features.Authentication.ResetPassword;

public record ResetPasswordCommand(string Email, string Token, string NewPassword, string ConfirmPassword) : ICommandQuery;
