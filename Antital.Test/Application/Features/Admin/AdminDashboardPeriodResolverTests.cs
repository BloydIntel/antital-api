using Antital.Application.Features.Admin.GetAdminDashboard;
using FluentAssertions;
using Xunit;

namespace Antital.Test.Application.Features.Admin;

public class AdminDashboardPeriodResolverTests
{
    private static readonly DateTimeOffset Now =
        new(2026, 10, 4, 18, 30, 0, TimeSpan.Zero);

    [Theory]
    [InlineData("last-7-days", 7)]
    [InlineData("last-30-days", 30)]
    [InlineData("last-90-days", 90)]
    public void TryResolve_SupportedPeriod_ReturnsEqualCurrentAndPreviousRanges(
        string period,
        int expectedDays)
    {
        var success = AdminDashboardPeriodResolver.TryResolve(
            period,
            Now,
            out var range,
            out var error);

        success.Should().BeTrue();
        error.Should().BeNull();
        range.CurrentEndUtc.Should().Be(Now.UtcDateTime);
        range.CurrentStartUtc.Should().Be(Now.UtcDateTime.AddDays(-expectedDays));
        range.PreviousEndUtc.Should().Be(range.CurrentStartUtc);
        range.PreviousStartUtc.Should().Be(range.CurrentStartUtc.AddDays(-expectedDays));
        (range.CurrentEndUtc - range.CurrentStartUtc)
            .Should().Be(range.PreviousEndUtc - range.PreviousStartUtc);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void TryResolve_MissingPeriod_UsesLastThirtyDays(string? period)
    {
        var success = AdminDashboardPeriodResolver.TryResolve(
            period,
            Now,
            out var range,
            out var error);

        success.Should().BeTrue();
        error.Should().BeNull();
        range.CurrentStartUtc.Should().Be(Now.UtcDateTime.AddDays(-30));
        range.CurrentEndUtc.Should().Be(Now.UtcDateTime);
    }

    [Fact]
    public void TryResolve_TrimsAndNormalizesPeriod()
    {
        var success = AdminDashboardPeriodResolver.TryResolve(
            "  LAST-7-DAYS  ",
            Now,
            out var range,
            out var error);

        success.Should().BeTrue();
        error.Should().BeNull();
        range.CurrentStartUtc.Should().Be(Now.UtcDateTime.AddDays(-7));
    }

    [Theory]
    [InlineData("this-month")]
    [InlineData("last-month")]
    [InlineData("last-7-day")]
    [InlineData("90")]
    public void TryResolve_UnsupportedPeriod_ReturnsValidationError(string period)
    {
        var success = AdminDashboardPeriodResolver.TryResolve(
            period,
            Now,
            out var range,
            out var error);

        success.Should().BeFalse();
        range.Should().Be(default(AdminDashboardPeriodRange));
        error.Should().Be(AdminDashboardPeriodResolver.ValidationMessage);
    }

    [Fact]
    public void TryResolve_NonUtcOffset_ConvertsAllBoundariesToUtc()
    {
        var localNow = new DateTimeOffset(2026, 10, 4, 19, 30, 0, TimeSpan.FromHours(1));

        var success = AdminDashboardPeriodResolver.TryResolve(
            "last-7-days",
            localNow,
            out var range,
            out var error);

        success.Should().BeTrue();
        error.Should().BeNull();
        range.CurrentEndUtc.Should().Be(Now.UtcDateTime);
        range.CurrentEndUtc.Kind.Should().Be(DateTimeKind.Utc);
        range.CurrentStartUtc.Kind.Should().Be(DateTimeKind.Utc);
        range.PreviousStartUtc.Kind.Should().Be(DateTimeKind.Utc);
        range.PreviousEndUtc.Kind.Should().Be(DateTimeKind.Utc);
    }

    [Fact]
    public void PeriodRanges_AreHalfOpenAndMeetWithoutOverlap()
    {
        AdminDashboardPeriodResolver.TryResolve(
            "last-30-days",
            Now,
            out var range,
            out _).Should().BeTrue();

        range.PreviousEndUtc.Should().Be(range.CurrentStartUtc);
        range.PreviousStartUtc.Should().BeBefore(range.PreviousEndUtc);
        range.CurrentStartUtc.Should().BeBefore(range.CurrentEndUtc);
    }
}
