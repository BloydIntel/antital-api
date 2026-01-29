using Antital.Application.Common.Security;
using Antital.Domain.Interfaces;
using BuildingBlocks.Application.Exceptions;
using BuildingBlocks.Application.Features;
using BuildingBlocks.Domain.Interfaces;
using BuildingBlocks.Resources;

namespace Antital.Application.Features.Users.UpdateUser;

public class UpdateUserCommandHandler(
    IUserRepository userRepository,
    IPasswordHasher passwordHasher,
    IAntitalUnitOfWork unitOfWork,
    ICurrentUser currentUser
) : ICommandQueryHandler<UpdateUserCommand>
{
    public async Task<Result> Handle(UpdateUserCommand request, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdAsync(request.Id, cancellationToken);
        if (user == null)
            throw new NotFoundException(Messages.NotFound);

        user.FirstName = request.FirstName;
        user.LastName = request.LastName;
        user.PreferredName = request.PreferredName;
        user.PhoneNumber = request.PhoneNumber ?? user.PhoneNumber;
        user.UserType = request.UserType;
        if (request.IsEmailVerified.HasValue)
            user.IsEmailVerified = request.IsEmailVerified.Value;

        if (!string.IsNullOrEmpty(request.Password))
        {
            user.PasswordHash = passwordHasher.HashPassword(request.Password);
        }

        var updatedBy = !string.IsNullOrEmpty(currentUser.UserName)
            ? currentUser.UserName
            : (!string.IsNullOrEmpty(currentUser.IPAddress) ? currentUser.IPAddress : "System");
        user.Updated(updatedBy);

        await userRepository.UpdateAsync(user, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        var result = new Result();
        result.OK();
        return result;
    }
}
