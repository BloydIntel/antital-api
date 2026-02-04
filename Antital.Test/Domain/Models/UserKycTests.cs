using Antital.Domain.Enums;
using Antital.Domain.Models;
using FluentAssertions;
using Xunit;

namespace Antital.Test.Domain.Models;

public class UserKycTests
{
    [Fact]
    public void UserKyc_Creation_WithAllProperties_ShouldSucceed()
    {
        // Arrange
        var userId = 1;
        var idType = KycIdType.NationalIdCard;
        var nin = "12345678901";
        var bvn = "12345678901";
        var govIdPath = "kyc/gov-id-123.jpg";
        var proofOfAddressPath = "kyc/address-123.pdf";
        var selfiePath = "kyc/selfie-123.jpg";
        var incomePath = "kyc/income-123.pdf";
        var govVerifiedAt = DateTime.UtcNow.AddDays(-1);
        var proofVerifiedAt = DateTime.UtcNow;

        // Act
        var entity = new UserKyc
        {
            UserId = userId,
            IdType = idType,
            Nin = nin,
            Bvn = bvn,
            GovernmentIdDocumentPathOrKey = govIdPath,
            ProofOfAddressDocumentPathOrKey = proofOfAddressPath,
            SelfieVerificationPathOrKey = selfiePath,
            IncomeVerificationPathOrKey = incomePath,
            GovernmentIdVerifiedAt = govVerifiedAt,
            ProofOfAddressVerifiedAt = proofVerifiedAt
        };

        // Assert
        entity.UserId.Should().Be(userId);
        entity.IdType.Should().Be(idType);
        entity.Nin.Should().Be(nin);
        entity.Bvn.Should().Be(bvn);
        entity.GovernmentIdDocumentPathOrKey.Should().Be(govIdPath);
        entity.ProofOfAddressDocumentPathOrKey.Should().Be(proofOfAddressPath);
        entity.SelfieVerificationPathOrKey.Should().Be(selfiePath);
        entity.IncomeVerificationPathOrKey.Should().Be(incomePath);
        entity.GovernmentIdVerifiedAt.Should().Be(govVerifiedAt);
        entity.ProofOfAddressVerifiedAt.Should().Be(proofVerifiedAt);
    }

    [Fact]
    public void UserKyc_Creation_WithOptionalNulls_ShouldSucceed()
    {
        // Act
        var entity = new UserKyc
        {
            UserId = 1,
            IdType = KycIdType.InternationalPassport
        };

        // Assert
        entity.UserId.Should().Be(1);
        entity.IdType.Should().Be(KycIdType.InternationalPassport);
        entity.Nin.Should().BeNull();
        entity.Bvn.Should().BeNull();
        entity.GovernmentIdDocumentPathOrKey.Should().BeNull();
        entity.ProofOfAddressDocumentPathOrKey.Should().BeNull();
        entity.GovernmentIdVerifiedAt.Should().BeNull();
    }

    [Fact]
    public void UserKyc_WithAllIdTypes_ShouldSetCorrectly()
    {
        foreach (KycIdType idType in Enum.GetValues(typeof(KycIdType)))
        {
            var entity = new UserKyc
            {
                UserId = 1,
                IdType = idType
            };
            entity.IdType.Should().Be(idType);
        }
    }
}
