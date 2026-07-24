using Antital.Application.DTOs.Fundraisers;
using Antital.Domain.Interfaces;
using BuildingBlocks.Application.Exceptions;
using BuildingBlocks.Application.Features;
using BuildingBlocks.Domain.Interfaces;

namespace Antital.Application.Features.Fundraisers.Investors.ReplyFundraiserInvestorMessage;

public record ReplyFundraiserInvestorMessageCommand(
    int MessageId,
    string Reply
) : ICommandQuery<FundraiserInvestorMessageDto>;

public class ReplyFundraiserInvestorMessageCommandHandler(
    IFundraiserUserAccess fundraiserUserAccess,
    IFundraiserDashboardRepository dashboardRepository,
    IFundraiserInvestorMessagesRepository messagesRepository,
    IAntitalUnitOfWork unitOfWork,
    ICurrentUser currentUser
) : ICommandQueryHandler<ReplyFundraiserInvestorMessageCommand, FundraiserInvestorMessageDto>
{
    public async Task<Result<FundraiserInvestorMessageDto>> Handle(
        ReplyFundraiserInvestorMessageCommand request,
        CancellationToken cancellationToken)
    {
        var reply = request.Reply?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(reply))
        {
            var invalid = new Result<FundraiserInvestorMessageDto>();
            invalid.BadRequest(
                "Reply is required.",
                new Dictionary<string, string[]> { ["reply"] = ["Reply is required."] });
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

        message.Reply = reply;
        message.RepliedAt = DateTime.UtcNow;
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
