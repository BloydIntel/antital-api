using Antital.Application.Features.Investors;
using FluentAssertions;
using Xunit;

namespace Antital.Test.Application.Features.Investors;

public class DashboardPeriodResolverTests
{
    [Theory]
    [InlineData("this-month")]
    [InlineData("last-month")]
    [InlineData("last-3-months")]
    [InlineData("last-6-months")]
    [InlineData("last-12-months")]
    public void TryResolve_ValidPeriods_ReturnsTrue(string period)
    {
        var success = DashboardPeriodResolver.TryResolve(period, out var range, out var error);

        success.Should().BeTrue();
        error.Should().BeNull();
        range.StartUtc.Should().BeBefore(range.EndUtc);
    }

    [Theory]
    [InlineData("active")]
    [InlineData("5")]
    [InlineData("2026-05")]
    [InlineData("last-3-month")]
    public void TryResolve_InvalidPeriods_ReturnsFalse(string period)
    {
        var success = DashboardPeriodResolver.TryResolve(period, out _, out var error);

        success.Should().BeFalse();
        error.Should().NotBeNullOrWhiteSpace();
    }
}
