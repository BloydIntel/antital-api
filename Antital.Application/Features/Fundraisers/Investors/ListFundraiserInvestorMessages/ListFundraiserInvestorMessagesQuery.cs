using Antital.Application.DTOs.Fundraisers;
using Antital.Domain.Interfaces;
using BuildingBlocks.Application.Features;

namespace Antital.Application.Features.Fundraisers.Investors.ListFundraiserInvestorMessages;

public record ListFundraiserInvestorMessagesQuery(
    string? Status = "all",
    int Page = 1,
    int PageSize = 20
) : ICommandQuery<FundraiserInvestorMessagesResponse>;

public class ListFundraiserInvestorMessagesQueryHandler(
    IFundraiserUserAccess fundraiserUserAccess,
    IFundraiserDashboardRepository dashboardRepository,
    IFundraiserInvestorMessagesRepository messagesRepository
) : ICommandQueryHandler<ListFundraiserInvestorMessagesQuery, FundraiserInvestorMessagesResponse>
{
    private const int MaxPageSize = 50;

    public async Task<Result<FundraiserInvestorMessagesResponse>> Handle(
        ListFundraiserInvestorMessagesQuery request,
        CancellationToken cancellationToken)
    {
        if (!FundraiserInvestorMessageMappers.TryParseMessageStatusFilter(
                request.Status,
                out var answered,
                out var statusError))
        {
            var invalid = new Result<FundraiserInvestorMessagesResponse>();
            invalid.BadRequest(
                "Invalid status.",
                new Dictionary<string, string[]> { ["status"] = [statusError!] });
            return invalid;
        }

        var (userId, _) = await fundraiserUserAccess.RequireFundraiserAsync(cancellationToken);
        var offering = await dashboardRepository.GetPrimaryOfferingAsync(userId, cancellationToken);

        var page = request.Page < 1 ? 1 : request.Page;
        var pageSize = request.PageSize < 1 ? 20 : Math.Min(request.PageSize, MaxPageSize);

        if (offering == null)
        {
            var empty = new Result<FundraiserInvestorMessagesResponse>();
            empty.AddValue(new FundraiserInvestorMessagesResponse([], page, pageSize, 0, 0));
            empty.OK();
            return empty;
        }

        var (items, totalCount, unansweredCount) = await messagesRepository.ListMessagesAsync(
            offering.Id,
            answered,
            page,
            pageSize,
            cancellationToken);

        var result = new Result<FundraiserInvestorMessagesResponse>();
        result.AddValue(new FundraiserInvestorMessagesResponse(
            items.Select(FundraiserInvestorMessageMappers.ToDto).ToList(),
            page,
            pageSize,
            totalCount,
            unansweredCount));
        result.OK();
        return result;
    }
}
