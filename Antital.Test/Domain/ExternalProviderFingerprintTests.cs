using Antital.Domain.Interfaces;
using FluentAssertions;
using Xunit;

namespace Antital.Test.Domain;

public class ExternalProviderFingerprintTests
{
    [Theory]
    [InlineData(null, null)]
    [InlineData("", null)]
    [InlineData("   ", null)]
    [InlineData("1234", "****")]
    [InlineData("22222222222", "*******2222")]
    [InlineData("A00123456", "*****3456")]
    public void Mask_KeepsLastFourWhenLongEnough(string? input, string? expected)
    {
        ExternalProviderFingerprint.Mask(input).Should().Be(expected);
    }
}
