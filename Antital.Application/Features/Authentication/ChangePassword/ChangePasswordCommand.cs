using BuildingBlocks.Application.Features;

namespace Antital.Application.Features.Authentication.ChangePassword;

public record ChangePasswordCommand(
    string CurrentPassword,
    string NewPassword,
    string ConfirmPassword
) : ICommandQuery;
