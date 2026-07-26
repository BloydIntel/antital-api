using Antital.Application.Services;
using Antital.Domain.Configuration;
using Antital.Domain.Enums;
using Antital.Domain.Interfaces;
using Antital.Domain.Models;
using BuildingBlocks.Application.Exceptions;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Antital.Test.Application.Services;

public class DojahKycVerificationServiceTests
{
    private readonly Mock<IDojahClient> _dojah = new();
    private readonly Mock<IUserRepository> _users = new();
    private readonly PassThroughKycVerificationService _passThrough = new();

    [Fact]
    public async Task ProcessAsync_WhenDisabled_PassThroughWithoutVerifiedAt()
    {
        var sut = CreateSut(enabled: false);
        var input = SampleInput();

        var result = await sut.ProcessAsync(input);

        result.GovernmentIdVerifiedAt.Should().BeNull();
        result.GovernmentIdDocumentPathOrKey.Should().Be("gov.png");
        _dojah.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ProcessAsync_WhenEnabledAndLookupsMatch_SetsGovernmentIdVerifiedAt()
    {
        var user = SampleUser();
        _users.Setup(u => u.GetByIdAsync(user.Id, It.IsAny<CancellationToken>())).ReturnsAsync(user);

        _dojah.Setup(d => d.LookupNinAsync("70123456789", It.IsAny<CancellationToken>()))
            .ReturnsAsync(SuccessIdentity("JOHN", "MUSA", "1990-01-15"));
        _dojah.Setup(d => d.LookupBvnAsync("22222222222", It.IsAny<CancellationToken>()))
            .ReturnsAsync(SuccessIdentity("JOHN", "MUSA", "1990-01-15"));

        var sut = CreateSut(enabled: true);
        var result = await sut.ProcessAsync(SampleInput());

        result.GovernmentIdVerifiedAt.Should().NotBeNull();
        result.GovernmentIdDocumentPathOrKey.Should().Be("gov.png");
    }

    [Fact]
    public async Task ProcessAsync_WhenBvnNameMismatch_ThrowsBadRequestOnBvn()
    {
        var user = SampleUser();
        _users.Setup(u => u.GetByIdAsync(user.Id, It.IsAny<CancellationToken>())).ReturnsAsync(user);

        _dojah.Setup(d => d.LookupNinAsync("70123456789", It.IsAny<CancellationToken>()))
            .ReturnsAsync(SuccessIdentity("JOHN", "MUSA", "1990-01-15"));
        _dojah.Setup(d => d.LookupBvnAsync("22222222222", It.IsAny<CancellationToken>()))
            .ReturnsAsync(SuccessIdentity("JANE", "MUSA", "1990-01-15"));

        var sut = CreateSut(enabled: true);
        var act = () => sut.ProcessAsync(SampleInput());

        var ex = await act.Should().ThrowAsync<BadRequestException>();
        ex.Which.Errors.Should().ContainKey("bvn");
    }

    [Fact]
    public async Task ProcessAsync_WhenBvnDobDiffersButNameMatches_Succeeds()
    {
        var user = SampleUser();
        _users.Setup(u => u.GetByIdAsync(user.Id, It.IsAny<CancellationToken>())).ReturnsAsync(user);

        _dojah.Setup(d => d.LookupNinAsync("70123456789", It.IsAny<CancellationToken>()))
            .ReturnsAsync(SuccessIdentity("JOHN", "MUSA", "1990-01-15"));
        // Sandbox BVN often returns a different DOB than NIN for the same name.
        _dojah.Setup(d => d.LookupBvnAsync("22222222222", It.IsAny<CancellationToken>()))
            .ReturnsAsync(SuccessIdentity("JOHN", "MUSA", "2000-05-01"));

        var sut = CreateSut(enabled: true);
        var result = await sut.ProcessAsync(SampleInput());

        result.GovernmentIdVerifiedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task ProcessAsync_WhenPassport_UsesSurnameFromProfile()
    {
        var user = SampleUser();
        _users.Setup(u => u.GetByIdAsync(user.Id, It.IsAny<CancellationToken>())).ReturnsAsync(user);

        _dojah.Setup(d => d.LookupPassportAsync("A00123456", "Musa", It.IsAny<CancellationToken>()))
            .ReturnsAsync(SuccessIdentity("JOHN", "MUSA", "15/01/1990"));
        _dojah.Setup(d => d.LookupBvnAsync("22222222222", It.IsAny<CancellationToken>()))
            .ReturnsAsync(SuccessIdentity("JOHN", "MUSA", "1990-01-15"));

        var sut = CreateSut(enabled: true);
        var input = SampleInput() with
        {
            IdType = (int)KycIdType.InternationalPassport,
            Nin = "A00123456",
        };

        var result = await sut.ProcessAsync(input);
        result.GovernmentIdVerifiedAt.Should().NotBeNull();
    }

    private DojahKycVerificationService CreateSut(bool enabled) =>
        new(
            _dojah.Object,
            _users.Object,
            Options.Create(new DojahSettings { Enabled = enabled }),
            _passThrough);

    private static User SampleUser() => new()
    {
        Id = 1,
        FirstName = "John",
        LastName = "Musa",
        DateOfBirth = new DateTime(1990, 1, 15, 0, 0, 0, DateTimeKind.Utc),
        Email = "john@example.com",
        PasswordHash = "x",
        PhoneNumber = "080",
        Nationality = "NG",
        CountryOfResidence = "NG",
        StateOfResidence = "LA",
        ResidentialAddress = "addr",
    };

    private static KycVerificationInput SampleInput() =>
        new(
            UserId: 1,
            IdType: (int)KycIdType.NationalIdCard,
            Nin: "70123456789",
            Bvn: "22222222222",
            GovernmentIdDocumentPathOrKey: "gov.png",
            ProofOfAddressDocumentPathOrKey: "proof.png",
            SelfieVerificationPathOrKey: null,
            IncomeVerificationPathOrKey: null,
            IncomeVerificationDocumentTypes: null);

    private static DojahIdentityLookupResult SuccessIdentity(string first, string last, string dob) =>
        new(true, 200, first, null, last, dob, null, "{}", null);
}
