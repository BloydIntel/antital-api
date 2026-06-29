using Antital.Application.Features.Investors;
using Antital.Domain.Interfaces;
using BuildingBlocks.Application.Exceptions;
using BuildingBlocks.Application.Features;
using BuildingBlocks.Domain.Interfaces;

namespace Antital.Application.Features.Investors.PaymentMethods.DeletePaymentMethod;

public class DeletePaymentMethodCommandHandler(
    IInvestorUserAccess investorUserAccess,
    IInvestorPaymentMethodRepository paymentMethodRepository,
    IAntitalUnitOfWork unitOfWork,
    ICurrentUser currentUser
) : ICommandQueryHandler<DeletePaymentMethodCommand>
{
    public async Task<Result> Handle(DeletePaymentMethodCommand request, CancellationToken cancellationToken)
    {
        var (userId, _) = await investorUserAccess.RequireAuthenticatedUserAsync(cancellationToken);

        var method = await paymentMethodRepository.GetByIdForUserAsync(request.PaymentMethodId, userId, cancellationToken);
        if (method == null)
        {
            throw new NotFoundException("Payment method not found.");
        }

        var wasDefault = method.IsDefault;
        var actor = ResolveActor();
        method.Deleted(actor);
        await paymentMethodRepository.UpdateAsync(method, cancellationToken);

        if (wasDefault)
        {
            await paymentMethodRepository.PromoteNextDefaultAsync(userId, method.Id, actor, cancellationToken);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        var result = new Result();
        result.OK();
        return result;
    }

    private string ResolveActor() =>
        !string.IsNullOrEmpty(currentUser.UserName)
            ? currentUser.UserName
            : (!string.IsNullOrEmpty(currentUser.IPAddress) ? currentUser.IPAddress : "System");
}
