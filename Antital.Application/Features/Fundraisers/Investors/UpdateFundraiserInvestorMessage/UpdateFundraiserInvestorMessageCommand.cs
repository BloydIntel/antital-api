using Antital.Application.DTOs.Fundraisers;
using Antital.Domain.Interfaces;
using BuildingBlocks.Application.Exceptions;
using BuildingBlocks.Application.Features;
using BuildingBlocks.Domain.Interfaces;

namespace Antital.Application.Features.Fundraisers.Investors.UpdateFundraiserInvestorMessage;

public record UpdateFundraiserInvestorMessageCommand(
    int MessageId,
    string? Visibility,
    string? Reply
) : ICommandQuery<FundraiserInvestorMessageDto>;

public class UpdateFundraiserInvestorMessageCommandHandler(
    IFundraiserUserAccess fundraiserUserAccess,
    IFundraiserDashboardRepository dashboardRepository,
    IFundraiserInvestorMessagesRepository messagesRepository,
    IAntitalUnitOfWork unitOfWork,
    ICurrentUser currentUser
) : ICommandQueryHandler<UpdateFundraiserInvestorMessageCommand, FundraiserInvestorMessageDto>
{
    public async Task<Result<FundraiserInvestorMessageDto>> Handle(
        UpdateFundraiserInvestorMessageCommand request,
        CancellationToken cancellationToken)
    {
        if (request.Visibility == null && request.Reply == null)
        {
            var invalid = new Result<FundraiserInvestorMessageDto>();
            invalid.BadRequest(
                "At least one field is required.",
                new Dictionary<string, string[]>
                {
                    ["body"] = ["Provide visibility and/or reply."],
                });
            return invalid;
        }

        var (userId, _) = await fundraiserUserAccess.RequireFundraiserAsync(cancellationToken);
        var offering = await dashboardRepository.GetPrimaryOfferingAsync(userId, cancellationToken);
        if (offering == null)
        {
            throw new NotFoundException("No owned fundraising campaign found.");
        }

        var message = await messagesRepository.GetByIdAsync(request.MessageId, cancellationToken);
        if (message == null || message.OfferingId != offering.Id)
        {
            throw new NotFoundException("Investor message not found.");
        }

        if (request.Visibility != null)
        {
            if (!FundraiserInvestorMessageMappers.TryParseVisibility(
                    request.Visibility,
                    out var visibility,
                    out var visibilityError))
            {
                var invalid = new Result<FundraiserInvestorMessageDto>();
                invalid.BadRequest(
                    "Invalid visibility.",
                    new Dictionary<string, string[]> { ["visibility"] = [visibilityError!] });
                return invalid;
            }

            message.Visibility = visibility;
        }

        if (request.Reply != null)
        {
            var reply = request.Reply.Trim();
            if (string.IsNullOrWhiteSpace(reply))
            {
                var invalid = new Result<FundraiserInvestorMessageDto>();
                invalid.BadRequest(
                    "Reply is required.",
                    new Dictionary<string, string[]> { ["reply"] = ["Reply is required."] });
                return invalid;
            }

            message.Reply = reply;
            message.RepliedAt ??= DateTime.UtcNow;
        }

        message.Updated(ResolveActor());
        await messagesRepository.UpdateAsync(message, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        var result = new Result<FundraiserInvestorMessageDto>();
        result.AddValue(FundraiserInvestorMessageMappers.ToDto(message));
        result.OK();
        return result;
    }

    private string ResolveActor() =>
        !string.IsNullOrEmpty(currentUser.UserName)
            ? currentUser.UserName
            : (!string.IsNullOrEmpty(currentUser.IPAddress) ? currentUser.IPAddress : "System");
}
