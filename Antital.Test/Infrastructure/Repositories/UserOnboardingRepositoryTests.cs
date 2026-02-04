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

public class UserOnboardingRepositoryTests : IDisposable
{
    private readonly AntitalDBContext _dbContext;
    private readonly Mock<ICurrentUser> _currentUserMock;
    private readonly UserOnboardingRepository _repository;

    public UserOnboardingRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<AntitalDBContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _dbContext = new AntitalDBContext(options);
        _currentUserMock = new Mock<ICurrentUser>();
        _currentUserMock.Setup(x => x.UserName).Returns("TestUser");
        _repository = new UserOnboardingRepository(_dbContext, _currentUserMock.Object);
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
        var onboarding = new UserOnboarding
        {
            UserId = user.Id,
            FlowType = OnboardingFlowType.IndividualInvestor,
            CurrentStep = OnboardingStep.InvestmentProfile,
            Status = OnboardingStatus.Draft
        };
        onboarding.Created("TestUser");
        _dbContext.UserOnboardings.Add(onboarding);
        await _dbContext.SaveChangesAsync(CancellationToken.None);

        var result = await _repository.GetByUserIdAsync(1, CancellationToken.None);

        result.Should().NotBeNull();
        result!.UserId.Should().Be(1);
        result.CurrentStep.Should().Be(OnboardingStep.InvestmentProfile);
    }

    [Fact]
    public async Task AddAsync_SavesEntity()
    {
        var user = await CreateUserAsync(1);
        var onboarding = new UserOnboarding
        {
            UserId = user.Id,
            FlowType = OnboardingFlowType.IndividualInvestor,
            CurrentStep = OnboardingStep.InvestorCategory,
            Status = OnboardingStatus.Draft
        };
        onboarding.Created("TestUser");

        await _repository.AddAsync(onboarding, CancellationToken.None);
        await _dbContext.SaveChangesAsync(CancellationToken.None);

        var saved = await _dbContext.UserOnboardings.FirstOrDefaultAsync(e => e.UserId == 1);
        saved.Should().NotBeNull();
        saved!.CurrentStep.Should().Be(OnboardingStep.InvestorCategory);
    }

    [Fact]
    public async Task UpdateAsync_UpdatesEntity()
    {
        var user = await CreateUserAsync(1);
        var onboarding = new UserOnboarding
        {
            UserId = user.Id,
            FlowType = OnboardingFlowType.IndividualInvestor,
            CurrentStep = OnboardingStep.InvestorCategory,
            Status = OnboardingStatus.Draft
        };
        onboarding.Created("TestUser");
        _dbContext.UserOnboardings.Add(onboarding);
        await _dbContext.SaveChangesAsync(CancellationToken.None);

        onboarding.CurrentStep = OnboardingStep.Kyc;
        onboarding.Updated("TestUser");
        await _repository.UpdateAsync(onboarding, CancellationToken.None);
        await _dbContext.SaveChangesAsync(CancellationToken.None);

        var updated = await _dbContext.UserOnboardings.FindAsync(onboarding.Id);
        updated.Should().NotBeNull();
        updated!.CurrentStep.Should().Be(OnboardingStep.Kyc);
    }

    [Fact]
    public async Task GetByUserIdAsync_DeletedEntity_ReturnsNull()
    {
        var user = await CreateUserAsync(1);
        var onboarding = new UserOnboarding
        {
            UserId = user.Id,
            FlowType = OnboardingFlowType.IndividualInvestor,
            CurrentStep = OnboardingStep.InvestorCategory,
            Status = OnboardingStatus.Draft
        };
        onboarding.Created("TestUser");
        onboarding.Deleted("TestUser");
        _dbContext.UserOnboardings.Add(onboarding);
        await _dbContext.SaveChangesAsync(CancellationToken.None);

        var result = await _repository.GetByUserIdAsync(1, CancellationToken.None);

        result.Should().BeNull();
    }

    public void Dispose() => _dbContext.Dispose();
}
