using Antital.Application.DTOs.Fundraisers;
using Antital.Domain.Enums;
using Antital.Domain.Interfaces;
using Antital.Domain.Models;
using BuildingBlocks.Application.Exceptions;
using BuildingBlocks.Application.Features;
using BuildingBlocks.Domain.Interfaces;

namespace Antital.Application.Features.Fundraisers.CampaignUpdates.CreateFundraiserCampaignUpdate;

public record CreateFundraiserCampaignUpdateCommand(
    string Title,
    string Body,
    bool Publish
) : ICommandQuery<FundraiserCampaignUpdateDto>;

public class CreateFundraiserCampaignUpdateCommandHandler(
    IFundraiserUserAccess fundraiserUserAccess,
    IFundraiserDashboardRepository dashboardRepository,
    IFundraiserCampaignUpdatesRepository updatesRepository,
    IAntitalUnitOfWork unitOfWork,
    ICurrentUser currentUser
) : ICommandQueryHandler<CreateFundraiserCampaignUpdateCommand, FundraiserCampaignUpdateDto>
{
    public async Task<Result<FundraiserCampaignUpdateDto>> Handle(
        CreateFundraiserCampaignUpdateCommand request,
        CancellationToken cancellationToken)
    {
        var title = request.Title?.Trim() ?? string.Empty;
        var body = request.Body?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(body))
        {
            var invalid = new Result<FundraiserCampaignUpdateDto>();
            invalid.BadRequest(
                "Title and body are required.",
                new Dictionary<string, string[]>
                {
                    ["title"] = string.IsNullOrWhiteSpace(title) ? ["Title is required."] : [],
                    ["body"] = string.IsNullOrWhiteSpace(body) ? ["Body is required."] : [],
                }.Where(kv => kv.Value.Length > 0).ToDictionary(kv => kv.Key, kv => kv.Value));
            return invalid;
        }

        var (userId, _) = await fundraiserUserAccess.RequireFundraiserAsync(cancellationToken);
        var offering = await dashboardRepository.GetPrimaryOfferingAsync(userId, cancellationToken);
        if (offering == null)
        {
            throw new NotFoundException("No owned fundraising campaign found.");
        }

        var now = DateTime.UtcNow;
        var update = new OfferingUpdate
        {
            OfferingId = offering.Id,
            Title = title,
            Body = body,
            Status = request.Publish ? OfferingUpdateStatus.Published : OfferingUpdateStatus.Draft,
            PublishedAt = request.Publish ? now : null,
            LikeCount = 0,
        };
        update.Created(ResolveActor());

        await updatesRepository.AddAsync(update, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        var result = new Result<FundraiserCampaignUpdateDto>();
        result.AddValue(FundraiserCampaignUpdateMappers.ToDto(update));
        result.OK();
        return result;
    }

    private string ResolveActor() =>
        !string.IsNullOrEmpty(currentUser.UserName)
            ? currentUser.UserName
            : (!string.IsNullOrEmpty(currentUser.IPAddress) ? currentUser.IPAddress : "System");
}
