using Antital.Application.DTOs.Investors;
using BuildingBlocks.Application.Features;

namespace Antital.Application.Features.Investors.GetInvestorDashboard;

public record GetInvestorDashboardQuery(string Period = "this-month") : ICommandQuery<InvestorDashboardResponse>;
