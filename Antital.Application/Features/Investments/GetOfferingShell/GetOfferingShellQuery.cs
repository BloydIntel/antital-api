using Antital.Application.DTOs.Investments;
using BuildingBlocks.Application.Features;

namespace Antital.Application.Features.Investments.GetOfferingShell;

public record GetOfferingShellQuery(string IdOrSlug) : ICommandQuery<OfferingShellResponse>;
