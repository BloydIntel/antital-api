using Antital.Application.Services;
using Antital.Domain.Configuration;
using Antital.Domain.Interfaces;
using BuildingBlocks.Application.Exceptions;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Antital.Test.Application.Services;

public class DojahCompanyVerificationServiceTests
{
    private readonly Mock<IDojahClient> _dojah = new();

    [Fact]
    public async Task VerifyFundraiserCompanyAsync_WhenDisabled_SkipsLookup()
    {
        var sut = CreateSut(enabled: false);

        await sut.VerifyFundraiserCompanyAsync(new FundraiserCompanyVerificationInput(
            "Acme Fundraise Limited",
            "LTD",
            "RC123456",
            new DateTime(2020, 1, 15)));

        _dojah.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task VerifyCorporateCompanyAsync_WhenLookupMatches_Succeeds()
    {
        _dojah.Setup(x => x.LookupCacAsync("RC123456", "COMPANY", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DojahCacLookupResult(
                true, 200, "Acme Ventures Limited", "RC123456", "COMPANY", "ACTIVE", "2020-01-15", "{}", null));

        var sut = CreateSut(enabled: true);

        await sut.VerifyCorporateCompanyAsync(new CorporateCompanyVerificationInput(
            "Acme Ventures Ltd",
            "LTD",
            "RC123456",
            new DateTime(2020, 1, 15)));
    }

    [Fact]
    public async Task VerifyCorporateCompanyAsync_WhenNameMismatches_ThrowsBadRequest()
    {
        _dojah.Setup(x => x.LookupCacAsync("RC123456", "COMPANY", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DojahCacLookupResult(
                true, 200, "Different Company Limited", "RC123456", "COMPANY", "ACTIVE", "2020-01-15", "{}", null));

        var sut = CreateSut(enabled: true);
        var act = () => sut.VerifyCorporateCompanyAsync(new CorporateCompanyVerificationInput(
            "Acme Ventures Ltd",
            "LTD",
            "RC123456",
            new DateTime(2020, 1, 15)));

        var ex = await act.Should().ThrowAsync<BadRequestException>();
        ex.Which.Errors.Should().ContainKey("companyLegalName");
    }

    [Fact]
    public async Task VerifyFundraiserCompanyAsync_WhenStatusInactive_ThrowsBadRequest()
    {
        _dojah.Setup(x => x.LookupCacAsync("BN998877", "BUSINESS_NAME", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DojahCacLookupResult(
                true, 200, "Acme Raise", "BN998877", "BUSINESS_NAME", "INACTIVE", "2020-01-15", "{}", null));

        var sut = CreateSut(enabled: true);
        var act = () => sut.VerifyFundraiserCompanyAsync(new FundraiserCompanyVerificationInput(
            "Acme Raise",
            "BN (Business Name)",
            "BN998877",
            new DateTime(2020, 1, 15)));

        var ex = await act.Should().ThrowAsync<BadRequestException>();
        ex.Which.Errors.Should().ContainKey("registrationNumber");
    }

    private DojahCompanyVerificationService CreateSut(bool enabled) =>
        new(_dojah.Object, Options.Create(new DojahSettings { Enabled = enabled }));
}
