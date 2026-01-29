using Antital.Domain.Enums;
using Antital.Domain.Models;
using FluentAssertions;
using Xunit;

namespace Antital.Test.Domain.Models;

public class UserTests
{
    [Fact]
    public void User_Creation_WithAllRequiredProperties_ShouldSucceed()
    {
        // Arrange
        var email = "test@example.com";
        var passwordHash = "hashed_password";
        var userType = UserTypeEnum.IndividualInvestor;
        var firstName = "John";
        var lastName = "Doe";
        var phoneNumber = "+1234567890";
        var dateOfBirth = new DateTime(1990, 1, 1);
        var nationality = "Nigerian";
        var countryOfResidence = "Nigeria";
        var stateOfResidence = "Lagos";
        var residentialAddress = "123 Main Street, Lagos";
        var hasAgreedToTerms = true;

        // Act
        var user = new User
        {
            Email = email,
            PasswordHash = passwordHash,
            UserType = userType,
            FirstName = firstName,
            LastName = lastName,
            PhoneNumber = phoneNumber,
            DateOfBirth = dateOfBirth,
            Nationality = nationality,
            CountryOfResidence = countryOfResidence,
            StateOfResidence = stateOfResidence,
            ResidentialAddress = residentialAddress,
            HasAgreedToTerms = hasAgreedToTerms
        };

        // Assert
        user.Email.Should().Be(email);
        user.PasswordHash.Should().Be(passwordHash);
        user.UserType.Should().Be(userType);
        user.FirstName.Should().Be(firstName);
        user.LastName.Should().Be(lastName);
        user.PhoneNumber.Should().Be(phoneNumber);
        user.DateOfBirth.Should().Be(dateOfBirth);
        user.Nationality.Should().Be(nationality);
        user.CountryOfResidence.Should().Be(countryOfResidence);
        user.StateOfResidence.Should().Be(stateOfResidence);
        user.ResidentialAddress.Should().Be(residentialAddress);
        user.HasAgreedToTerms.Should().BeTrue();
    }

    [Fact]
    public void User_Creation_WithOptionalPreferredName_ShouldSucceed()
    {
        // Arrange
        var preferredName = "Johnny";

        // Act
        var user = new User
        {
            Email = "test@example.com",
            PasswordHash = "hashed_password",
            UserType = UserTypeEnum.IndividualInvestor,
            FirstName = "John",
            LastName = "Doe",
            PreferredName = preferredName,
            PhoneNumber = "+1234567890",
            DateOfBirth = new DateTime(1990, 1, 1),
            Nationality = "Nigerian",
            CountryOfResidence = "Nigeria",
            StateOfResidence = "Lagos",
            ResidentialAddress = "123 Main Street, Lagos",
            HasAgreedToTerms = true
        };

        // Assert
        user.PreferredName.Should().Be(preferredName);
    }

    [Fact]
    public void User_Creation_Defaults_IsEmailVerified_ToFalse()
    {
        // Act
        var user = new User
        {
            Email = "test@example.com",
            PasswordHash = "hashed_password",
            UserType = UserTypeEnum.IndividualInvestor,
            FirstName = "John",
            LastName = "Doe",
            PhoneNumber = "+1234567890",
            DateOfBirth = new DateTime(1990, 1, 1),
            Nationality = "Nigerian",
            CountryOfResidence = "Nigeria",
            StateOfResidence = "Lagos",
            ResidentialAddress = "123 Main Street, Lagos",
            HasAgreedToTerms = true
        };

        // Assert
        user.IsEmailVerified.Should().BeFalse();
    }

    [Fact]
    public void User_Creation_WithEmailVerificationToken_ShouldStoreToken()
    {
        // Arrange
        var verificationToken = "verification_token_12345";
        var tokenExpiry = DateTime.UtcNow.AddHours(24);

        // Act
        var user = new User
        {
            Email = "test@example.com",
            PasswordHash = "hashed_password",
            UserType = UserTypeEnum.IndividualInvestor,
            FirstName = "John",
            LastName = "Doe",
            PhoneNumber = "+1234567890",
            DateOfBirth = new DateTime(1990, 1, 1),
            Nationality = "Nigerian",
            CountryOfResidence = "Nigeria",
            StateOfResidence = "Lagos",
            ResidentialAddress = "123 Main Street, Lagos",
            HasAgreedToTerms = true,
            EmailVerificationToken = verificationToken,
            EmailVerificationTokenExpiry = tokenExpiry
        };

        // Assert
        user.EmailVerificationToken.Should().Be(verificationToken);
        user.EmailVerificationTokenExpiry.Should().Be(tokenExpiry);
    }

    [Fact]
    public void User_Creation_WithDifferentUserTypes_ShouldSetCorrectly()
    {
        // Test IndividualInvestor
        var individualUser = new User
        {
            Email = "individual@example.com",
            PasswordHash = "hashed_password",
            UserType = UserTypeEnum.IndividualInvestor,
            FirstName = "John",
            LastName = "Doe",
            PhoneNumber = "+1234567890",
            DateOfBirth = new DateTime(1990, 1, 1),
            Nationality = "Nigerian",
            CountryOfResidence = "Nigeria",
            StateOfResidence = "Lagos",
            ResidentialAddress = "123 Main Street, Lagos",
            HasAgreedToTerms = true
        };
        individualUser.UserType.Should().Be(UserTypeEnum.IndividualInvestor);

        // Test CorporateInvestor
        var corporateUser = new User
        {
            Email = "corporate@example.com",
            PasswordHash = "hashed_password",
            UserType = UserTypeEnum.CorporateInvestor,
            FirstName = "Company",
            LastName = "Inc",
            PhoneNumber = "+1234567890",
            DateOfBirth = new DateTime(2000, 1, 1),
            Nationality = "Nigerian",
            CountryOfResidence = "Nigeria",
            StateOfResidence = "Lagos",
            ResidentialAddress = "123 Main Street, Lagos",
            HasAgreedToTerms = true
        };
        corporateUser.UserType.Should().Be(UserTypeEnum.CorporateInvestor);

        // Test FundRaiser
        var fundRaiserUser = new User
        {
            Email = "fundraiser@example.com",
            PasswordHash = "hashed_password",
            UserType = UserTypeEnum.FundRaiser,
            FirstName = "Startup",
            LastName = "Co",
            PhoneNumber = "+1234567890",
            DateOfBirth = new DateTime(2010, 1, 1),
            Nationality = "Nigerian",
            CountryOfResidence = "Nigeria",
            StateOfResidence = "Lagos",
            ResidentialAddress = "123 Main Street, Lagos",
            HasAgreedToTerms = true
        };
        fundRaiserUser.UserType.Should().Be(UserTypeEnum.FundRaiser);
    }
}
