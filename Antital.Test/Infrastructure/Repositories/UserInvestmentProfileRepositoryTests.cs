using Antital.Domain.Enums;
using Antital.Domain.Models;
using Antital.Infrastructure;
using Antital.Infrastructure.Repositories;
using BuildingBlocks.Domain.Interfaces;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace Antital.Test.Infrastructure.Repositories;

public class UserInvestmentProfileRepositoryTests : IDisposable
{
    private readonly AntitalDBContext _dbContext;
    private readonly Mock<ICurrentUser> _currentUserMock;
    private readonly UserInvestmentProfileRepository _repository;

    public UserInvestmentProfileRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<AntitalDBContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _dbContext = new AntitalDBContext(options);
        _currentUserMock = new Mock<ICurrentUser>();
        _currentUserMock.Setup(x => x.UserName).Returns("TestUser");
        _repository = new UserInvestmentProfileRepository(_dbContext, _currentUserMock.Object);
    }

    private async Task<User> CreateUserAsync(int id = 1)
    {
        var user = new User
        {
            Id = id,
            Email = $"user{id}@test.com",
            PasswordHash = "hash",
            UserType = UserTypeEnum.IndividualInvestor,
            FirstName = "A",
            LastName = "B",
            PhoneNumber = "+1",
            DateOfBirth = DateTime.UtcNow,
            Nationality = "NG",
            CountryOfResidence = "NG",
            StateOfResidence = "Lagos",
            ResidentialAddress = "Addr",
            HasAgreedToTerms = true
        };
        user.Created("TestUser");
        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync(CancellationToken.None);
        return user;
    }

    [Fact]
    public async Task GetByUserIdAsync_NoRecord_ReturnsNull()
    {
        await CreateUserAsync(1);

        var result = await _repository.GetByUserIdAsync(1, CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByUserIdAsync_ExistingRecord_ReturnsEntity()
    {
        var user = await CreateUserAsync(1);
        var profile = new UserInvestmentProfile
        {
            UserId = user.Id,
            InvestorCategory = InvestorCategory.Retail,
            AnnualIncomeRange = "N5m-N10m"
        };
        profile.Created("TestUser");
        _dbContext.UserInvestmentProfiles.Add(profile);
        await _dbContext.SaveChangesAsync(CancellationToken.None);

        var result = await _repository.GetByUserIdAsync(1, CancellationToken.None);

        result.Should().NotBeNull();
        result!.UserId.Should().Be(1);
        result.InvestorCategory.Should().Be(InvestorCategory.Retail);
        result.AnnualIncomeRange.Should().Be("N5m-N10m");
    }

    [Fact]
    public async Task AddAsync_SavesEntity()
    {
        var user = await CreateUserAsync(1);
        var profile = new UserInvestmentProfile
        {
            UserId = user.Id,
            InvestorCategory = InvestorCategory.Sophisticated,
            HighRiskAllocationPast12MonthsPercent = 10m
        };

        await _repository.AddAsync(profile, CancellationToken.None);
        await _dbContext.SaveChangesAsync(CancellationToken.None);

        var saved = await _dbContext.UserInvestmentProfiles.FirstOrDefaultAsync(e => e.UserId == 1);
        saved.Should().NotBeNull();
        saved!.InvestorCategory.Should().Be(InvestorCategory.Sophisticated);
    }

    [Fact]
    public async Task UpdateAsync_UpdatesEntity()
    {
        var user = await CreateUserAsync(1);
        var profile = new UserInvestmentProfile
        {
            UserId = user.Id,
            InvestorCategory = InvestorCategory.Retail
        };
        profile.Created("TestUser");
        _dbContext.UserInvestmentProfiles.Add(profile);
        await _dbContext.SaveChangesAsync(CancellationToken.None);

        profile.InvestorCategory = InvestorCategory.HighNetWorth;
        await _repository.UpdateAsync(profile, CancellationToken.None);
        await _dbContext.SaveChangesAsync(CancellationToken.None);

        var updated = await _dbContext.UserInvestmentProfiles.FindAsync(profile.Id);
        updated.Should().NotBeNull();
        updated!.InvestorCategory.Should().Be(InvestorCategory.HighNetWorth);
    }

    [Fact]
    public async Task GetByUserIdAsync_DeletedEntity_ReturnsNull()
    {
        var user = await CreateUserAsync(1);
        var profile = new UserInvestmentProfile { UserId = user.Id, InvestorCategory = InvestorCategory.Retail };
        profile.Created("TestUser");
        profile.Deleted("TestUser");
        _dbContext.UserInvestmentProfiles.Add(profile);
        await _dbContext.SaveChangesAsync(CancellationToken.None);

        var result = await _repository.GetByUserIdAsync(1, CancellationToken.None);

        result.Should().BeNull();
    }

    public void Dispose() => _dbContext.Dispose();
}
