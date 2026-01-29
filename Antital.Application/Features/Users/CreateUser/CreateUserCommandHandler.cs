using Antital.Application.DTOs;
using Antital.Application.Common.Security;
using Antital.Domain.Interfaces;
using Antital.Domain.Models;
using BuildingBlocks.Application.Exceptions;
using BuildingBlocks.Application.Features;
using BuildingBlocks.Domain.Interfaces;
using BuildingBlocks.Resources;

namespace Antital.Application.Features.Users.CreateUser;

public class CreateUserCommandHandler(
    IUserRepository userRepository,
    IPasswordHasher passwordHasher,
    IAntitalUnitOfWork unitOfWork,
    ICurrentUser currentUser
) : ICommandQueryHandler<CreateUserCommand, UserDto>
{
    public async Task<Result<UserDto>> Handle(CreateUserCommand request, CancellationToken cancellationToken)
    {
        if (await userRepository.EmailExistsAsync(request.Email, cancellationToken))
            throw new ConflictException(Messages.Conflict);

        var passwordHash = passwordHasher.HashPassword(request.Password);

        var user = new User
        {
            Email = request.Email,
            PasswordHash = passwordHash,
            FirstName = request.FirstName,
            LastName = request.LastName,
            PreferredName = request.PreferredName,
            PhoneNumber = request.PhoneNumber,
            UserType = request.UserType,
            IsEmailVerified = false,
            HasAgreedToTerms = true // assume admin-created users have accepted terms operationally
        };

        var createdBy = !string.IsNullOrEmpty(currentUser.UserName)
            ? currentUser.UserName
            : (!string.IsNullOrEmpty(currentUser.IPAddress) ? currentUser.IPAddress : "System");
        user.Created(createdBy);

        await userRepository.AddAsync(user, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        var dto = new UserDto
        {
            Id = user.Id,
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName,
            PreferredName = user.PreferredName,
            PhoneNumber = user.PhoneNumber,
            UserType = user.UserType,
            IsEmailVerified = user.IsEmailVerified
        };

        var result = new Result<UserDto>();
        result.AddValue(dto);
        result.OK();
        return result;
    }
}
