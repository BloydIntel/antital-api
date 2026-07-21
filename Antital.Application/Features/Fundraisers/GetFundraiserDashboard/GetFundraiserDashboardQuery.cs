using Antital.Application.DTOs.Fundraisers;
using BuildingBlocks.Application.Features;

namespace Antital.Application.Features.Fundraisers.GetFundraiserDashboard;

public record GetFundraiserDashboardQuery(string Period = "this-month") : ICommandQuery<FundraiserDashboardResponse>;
