using Antital.Application.DTOs;
using Antital.Domain.Interfaces;
using BuildingBlocks.Application.Exceptions;
using BuildingBlocks.Application.Features;
using BuildingBlocks.Resources;

namespace Antital.Application.Features.Users.GetUserById;

public class GetUserByIdQueryHandler(
    IUserRepository userRepository,
    IUserInvestmentProfileRepository userInvestmentProfileRepository
) : ICommandQueryHandler<GetUserByIdQuery, UserDto>
{
    public async Task<Result<UserDto>> Handle(GetUserByIdQuery request, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdAsync(request.Id, cancellationToken);
        if (user == null)
            throw new NotFoundException(Messages.NotFound);

        var profile = await userInvestmentProfileRepository.GetByUserIdAsync(user.Id, cancellationToken);

        var dto = new UserDto
        {
            Id = user.Id,
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName,
            PhoneNumber = user.PhoneNumber,
            PreferredName = user.PreferredName,
            UserType = user.UserType,
            IsEmailVerified = user.IsEmailVerified,
            DateOfBirth = user.DateOfBirth == DateTime.MinValue ? null : user.DateOfBirth,
            Nationality = NullIfEmpty(user.Nationality),
            CountryOfResidence = NullIfEmpty(user.CountryOfResidence),
            StateOfResidence = NullIfEmpty(user.StateOfResidence),
            ResidentialAddress = NullIfEmpty(user.ResidentialAddress),
            HasAgreedToTerms = user.HasAgreedToTerms,
            Company = profile is null
                ? null
                : new UserCompanyDto
                {
                    CompanyLegalName = profile.CompanyLegalName,
                    TradingBrandName = profile.TradingBrandName,
                    RegistrationType = profile.RegistrationType,
                    RegistrationNumber = profile.RegistrationNumber,
                    CompanyLoginEmail = profile.CompanyLoginEmail,
                    DateOfRegistration = profile.DateOfRegistration,
                    CompanyWebsite = profile.CompanyWebsite,
                    BusinessAddress = profile.BusinessAddress,
                    RegisteredAddress = profile.RegisteredAddress,
                    CompanyEmail = profile.CompanyEmail,
                    CompanyPhone = profile.CompanyPhone,
                    RepresentativeFullName = profile.RepresentativeFullName,
                    RepresentativeJobTitle = profile.RepresentativeJobTitle,
                    RepresentativePhoneNumber = profile.RepresentativePhoneNumber,
                    RepresentativeDateOfBirth = profile.RepresentativeDateOfBirth,
                    RepresentativeEmail = profile.RepresentativeEmail,
                    RepresentativeNationality = profile.RepresentativeNationality,
                    RepresentativeCountryOfResidence = profile.RepresentativeCountryOfResidence,
                    RepresentativeAddress = profile.RepresentativeAddress
                }
        };

        var result = new Result<UserDto>();
        result.AddValue(dto);
        result.OK();
        return result;
    }

    private static string? NullIfEmpty(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value;
}
