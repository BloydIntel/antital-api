using Antital.Application.Features.Onboarding;
using Antital.Domain.Configuration;
using BuildingBlocks.Application.Features;
using Microsoft.Extensions.Options;

namespace Antital.Application.Features.Onboarding.GetDojahWidgetConfig;

public record GetDojahWidgetConfigQuery : ICommandQuery<DojahWidgetConfigResponse>;

public record DojahWidgetConfigResponse(
    bool Enabled,
    string AppId,
    string PublicKey,
    string WidgetId
);

public class GetDojahWidgetConfigQueryHandler(
    IOnboardingUserAccess userAccess,
    IOptions<DojahSettings> dojahOptions
) : ICommandQueryHandler<GetDojahWidgetConfigQuery, DojahWidgetConfigResponse>
{
    public async Task<Result<DojahWidgetConfigResponse>> Handle(
        GetDojahWidgetConfigQuery request,
        CancellationToken cancellationToken)
    {
        await userAccess.RequireVerifiedUserAsync(cancellationToken);
        var settings = dojahOptions.Value;

        var result = new Result<DojahWidgetConfigResponse>();
        result.AddValue(new DojahWidgetConfigResponse(
            settings.Enabled,
            settings.AppId ?? string.Empty,
            settings.PublicKey ?? string.Empty,
            settings.WidgetId ?? string.Empty));
        result.OK();
        return result;
    }
}
