using Antital.Application.DTOs.Fundraisers;
using Antital.Domain.Enums;
using Antital.Domain.Interfaces;
using BuildingBlocks.Application.Exceptions;
using BuildingBlocks.Application.Features;
using BuildingBlocks.Domain.Interfaces;

namespace Antital.Application.Features.Fundraisers.CampaignUpdates.UpdateFundraiserCampaignUpdate;

public record UpdateFundraiserCampaignUpdateCommand(
    int UpdateId,
    string? Title,
    string? Body,
    bool? Publish
) : ICommandQuery<FundraiserCampaignUpdateDto>;

public class UpdateFundraiserCampaignUpdateCommandHandler(
    IFundraiserUserAccess fundraiserUserAccess,
    IFundraiserDashboardRepository dashboardRepository,
    IFundraiserCampaignUpdatesRepository updatesRepository,
    IAntitalUnitOfWork unitOfWork,
    ICurrentUser currentUser
) : ICommandQueryHandler<UpdateFundraiserCampaignUpdateCommand, FundraiserCampaignUpdateDto>
{
    public async Task<Result<FundraiserCampaignUpdateDto>> Handle(
        UpdateFundraiserCampaignUpdateCommand request,
        CancellationToken cancellationToken)
    {
        var (userId, _) = await fundraiserUserAccess.RequireFundraiserAsync(cancellationToken);
        var offering = await dashboardRepository.GetPrimaryOfferingAsync(userId, cancellationToken);
        if (offering == null)
        {
            throw new NotFoundException("No owned fundraising campaign found.");
        }

        var update = await updatesRepository.GetByIdAsync(request.UpdateId, cancellationToken);
        if (update == null || update.OfferingId != offering.Id)
        {
            throw new NotFoundException("Campaign update not found.");
        }

        if (request.Title != null)
        {
            var title = request.Title.Trim();
            if (string.IsNullOrWhiteSpace(title))
            {
                var invalid = new Result<FundraiserCampaignUpdateDto>();
                invalid.BadRequest(
                    "Title is required.",
                    new Dictionary<string, string[]> { ["title"] = ["Title is required."] });
                return invalid;
            }

            update.Title = title;
        }

        if (request.Body != null)
        {
            var body = request.Body.Trim();
            if (string.IsNullOrWhiteSpace(body))
            {
                var invalid = new Result<FundraiserCampaignUpdateDto>();
                invalid.BadRequest(
                    "Body is required.",
                    new Dictionary<string, string[]> { ["body"] = ["Body is required."] });
                return invalid;
            }

            update.Body = body;
        }

        if (request.Publish == true && update.Status != OfferingUpdateStatus.Published)
        {
            update.Status = OfferingUpdateStatus.Published;
            update.PublishedAt = DateTime.UtcNow;
        }

        update.Updated(ResolveActor());
        await updatesRepository.UpdateAsync(update, cancellationToken);
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
