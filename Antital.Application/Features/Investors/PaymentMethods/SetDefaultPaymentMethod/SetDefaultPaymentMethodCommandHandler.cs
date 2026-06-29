using Antital.Application.DTOs.Investors;
using Antital.Application.Features.Investors;
using Antital.Application.Features.Investors.PaymentMethods;
using Antital.Domain.Interfaces;
using BuildingBlocks.Application.Exceptions;
using BuildingBlocks.Application.Features;
using BuildingBlocks.Domain.Interfaces;

namespace Antital.Application.Features.Investors.PaymentMethods.SetDefaultPaymentMethod;

public class SetDefaultPaymentMethodCommandHandler(
    IInvestorUserAccess investorUserAccess,
    IInvestorPaymentMethodRepository paymentMethodRepository,
    IAntitalUnitOfWork unitOfWork,
    ICurrentUser currentUser
) : ICommandQueryHandler<SetDefaultPaymentMethodCommand, PaymentMethodResponse>
{
    public async Task<Result<PaymentMethodResponse>> Handle(
        SetDefaultPaymentMethodCommand request,
        CancellationToken cancellationToken)
    {
        var (userId, _) = await investorUserAccess.RequireAuthenticatedUserAsync(cancellationToken);

        var method = await paymentMethodRepository.GetByIdForUserAsync(request.PaymentMethodId, userId, cancellationToken);
        if (method == null)
        {
            throw new NotFoundException("Payment method not found.");
        }

        if (!method.IsDefault)
        {
            await paymentMethodRepository.ClearDefaultForUserAsync(userId, cancellationToken);
            method.IsDefault = true;
            method.Updated(ResolveActor());
            await paymentMethodRepository.UpdateAsync(method, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }

        var item = PaymentMethodMapper.ToItem(method);
        var response = new PaymentMethodResponse(item);
        var result = new Result<PaymentMethodResponse>();
        result.AddValue(response);
        result.OK();
        return result;
    }

    private string ResolveActor() =>
        !string.IsNullOrEmpty(currentUser.UserName)
            ? currentUser.UserName
            : (!string.IsNullOrEmpty(currentUser.IPAddress) ? currentUser.IPAddress : "System");
}
