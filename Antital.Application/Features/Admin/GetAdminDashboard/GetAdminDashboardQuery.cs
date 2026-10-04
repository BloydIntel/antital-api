using Antital.Application.DTOs.Admin;
using BuildingBlocks.Application.Features;

namespace Antital.Application.Features.Admin.GetAdminDashboard;

public record GetAdminDashboardQuery(string Period = AdminDashboardPeriodResolver.DefaultPeriod)
    : ICommandQuery<AdminDashboardResponse>;
