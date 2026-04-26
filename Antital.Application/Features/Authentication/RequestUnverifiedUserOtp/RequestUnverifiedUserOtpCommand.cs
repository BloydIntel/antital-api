using BuildingBlocks.Application.Features;

namespace Antital.Application.Features.Authentication.RequestUnverifiedUserOtp;

public record RequestUnverifiedUserOtpCommand(
    string Email
) : ICommandQuery;
