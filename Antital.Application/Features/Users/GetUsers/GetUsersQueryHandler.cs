using Antital.Application.DTOs;
using Antital.Domain.Interfaces;
using BuildingBlocks.Application.Features;

namespace Antital.Application.Features.Users.GetUsers;

public class GetUsersQueryHandler(IUserRepository userRepository) : ICommandQueryHandler<GetUsersQuery, List<UserDto>>
{
    public async Task<Result<List<UserDto>>> Handle(GetUsersQuery request, CancellationToken cancellationToken)
    {
        var users = await userRepository.GetAllAsync(cancellationToken);

        var list = users
            .Select(u => new UserDto
            {
                Id = u.Id,
                Email = u.Email,
                FirstName = u.FirstName,
                LastName = u.LastName,
                PhoneNumber = u.PhoneNumber,
                PreferredName = u.PreferredName,
                UserType = u.UserType,
                IsEmailVerified = u.IsEmailVerified
            })
            .ToList();

        var result = new Result<List<UserDto>>();
        result.AddValue(list);
        result.OK();
        return result;
    }
}
