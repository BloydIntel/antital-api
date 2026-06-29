using Antital.Application.DTOs.Investors;
using Antital.Domain.Models;

namespace Antital.Application.Features.Investors.Profile;

internal static class InvestorProfileMapper
{
    public static InvestorProfileResponse ToResponse(User user) =>
        new(
            user.Id,
            user.Email,
            user.FirstName,
            user.LastName,
            user.PreferredName,
            user.PhoneNumber,
            user.ResidentialAddress,
            user.StateOfResidence,
            user.CountryOfResidence,
            user.DateOfBirth,
            user.Nationality,
            user.UserType,
            user.IsEmailVerified);

    public static void ApplyUpdate(User user, UpdateInvestorProfileRequest request)
    {
        user.FirstName = request.FirstName;
        user.LastName = request.LastName;
        user.PreferredName = request.PreferredName;
        user.PhoneNumber = request.PhoneNumber;
        user.ResidentialAddress = request.ResidentialAddress;
        user.StateOfResidence = request.StateOfResidence;
        user.CountryOfResidence = request.CountryOfResidence;
    }
}
