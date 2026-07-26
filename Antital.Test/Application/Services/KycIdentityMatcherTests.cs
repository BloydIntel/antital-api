using Antital.Application.Services;
using FluentAssertions;
using Xunit;

namespace Antital.Test.Application.Services;

public class KycIdentityMatcherTests
{
    [Theory]
    [InlineData("John", "Musa", "JOHN", "MUSA", true)]
    [InlineData("John", "Musa", "JOHN DOE", "MUSA", true)]
    [InlineData("John", "Musa", "JANE", "MUSA", false)]
    [InlineData("John", "Musa", "JOHN", "ADAMU", false)]
    [InlineData("Mary-Jane", "O'Brien", "MARYJANE", "OBRIEN", true)]
    public void NamesMatch_AppliesNormalizedRules(
        string profileFirst,
        string profileLast,
        string providerFirst,
        string providerLast,
        bool expected)
    {
        KycIdentityMatcher.NamesMatch(profileFirst, profileLast, providerFirst, providerLast)
            .Should()
            .Be(expected);
    }

    [Fact]
    public void DatesOfBirthMatch_AcceptsCommonProviderFormats()
    {
        var profile = new DateTime(1997, 5, 16, 0, 0, 0, DateTimeKind.Utc);

        KycIdentityMatcher.DatesOfBirthMatch(profile, "1997-05-16").Should().BeTrue();
        KycIdentityMatcher.DatesOfBirthMatch(profile, "16-05-1997").Should().BeTrue();
        KycIdentityMatcher.DatesOfBirthMatch(profile, "16/05/1997").Should().BeTrue();
        KycIdentityMatcher.DatesOfBirthMatch(profile, null).Should().BeTrue();
        KycIdentityMatcher.DatesOfBirthMatch(profile, "1998-05-16").Should().BeFalse();
    }
}
