using Antital.Application.DTOs.Investors;
using Antital.Application.Features.Investors;
using Antital.Application.Features.Investors.PaymentMethods;
using Antital.Domain.Interfaces;
using Antital.Domain.Models;
using BuildingBlocks.Application.Exceptions;
using BuildingBlocks.Application.Features;
using BuildingBlocks.Domain.Interfaces;

namespace Antital.Application.Features.Investors.PaymentMethods.AddPaymentMethod;

public class AddPaymentMethodCommandHandler(
    IInvestorUserAccess investorUserAccess,
    IInvestorPaymentMethodRepository paymentMethodRepository,
    IAntitalUnitOfWork unitOfWork,
    ICurrentUser currentUser
) : ICommandQueryHandler<AddPaymentMethodCommand, PaymentMethodResponse>
{
    public async Task<Result<PaymentMethodResponse>> Handle(
        AddPaymentMethodCommand request,
        CancellationToken cancellationToken)
    {
        var (userId, _) = await investorUserAccess.RequireAuthenticatedUserAsync(cancellationToken);
        if (!PaymentMethodMapper.TryParseType(request.Type, out var type))
        {
            throw new BadRequestException(
                "Type must be Bank or Card.",
                new Dictionary<string, string[]> { ["type"] = ["Type must be Bank or Card."] });
        }

        var existing = await paymentMethodRepository.ListByUserAsync(userId, cancellationToken);
        var setAsDefault = request.SetAsDefault || existing.Count == 0;

        if (setAsDefault)
        {
            await paymentMethodRepository.ClearDefaultForUserAsync(userId, cancellationToken);
        }

        var actor = ResolveActor();
        var providerName = request.ProviderName.Trim();
        var last4 = request.Last4.Trim();

        var method = new InvestorPaymentMethod
        {
            UserId = userId,
            Type = type,
            Title = request.Title.Trim(),
            ProviderName = providerName,
            Last4 = last4,
            Subtitle = PaymentMethodMapper.BuildSubtitle(type, providerName, last4),
            IsDefault = setAsDefault,
            IsVerified = true,
        };
        method.Created(actor);

        await paymentMethodRepository.AddAsync(method, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

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
