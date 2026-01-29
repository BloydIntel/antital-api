using Antital.Application.DTOs;
using Antital.Domain.Interfaces;
using BuildingBlocks.Application.Exceptions;
using BuildingBlocks.Application.Features;
using BuildingBlocks.Resources;

namespace Antital.Application.Features.Users.GetUserById;

public class GetUserByIdQueryHandler(IUserRepository userRepository) : ICommandQueryHandler<GetUserByIdQuery, UserDto>
{
    public async Task<Result<UserDto>> Handle(GetUserByIdQuery request, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdAsync(request.Id, cancellationToken);
        if (user == null)
            throw new NotFoundException(Messages.NotFound);

        var dto = new UserDto
        {
            Id = user.Id,
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName,
            PhoneNumber = user.PhoneNumber,
            PreferredName = user.PreferredName,
            UserType = user.UserType,
            IsEmailVerified = user.IsEmailVerified
        };

        var result = new Result<UserDto>();
        result.AddValue(dto);
        result.OK();
        return result;
    }
}
