using Antital.Application.DTOs.Investments;
using BuildingBlocks.Application.Features;

namespace Antital.Application.Features.Investments.ListInvestments;

public record ListInvestmentsQuery(
    int Page = 1,
    int PageSize = 12,
    string? Category = null,
    string? Risk = null,
    string? Search = null) : ICommandQuery<InvestmentListResponse>;
