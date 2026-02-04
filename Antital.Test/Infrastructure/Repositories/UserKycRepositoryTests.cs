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

public class UserKycRepositoryTests : IDisposable
{
    private readonly AntitalDBContext _dbContext;
    private readonly Mock<ICurrentUser> _currentUserMock;
    private readonly UserKycRepository _repository;

    public UserKycRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<AntitalDBContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _dbContext = new AntitalDBContext(options);
        _currentUserMock = new Mock<ICurrentUser>();
        _currentUserMock.Setup(x => x.UserName).Returns("TestUser");
        _repository = new UserKycRepository(_dbContext, _currentUserMock.Object);
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
        var kyc = new UserKyc
        {
            UserId = user.Id,
            IdType = KycIdType.NationalIdCard,
            Nin = "12345678901",
            Bvn = "12345678901"
        };
        kyc.Created("TestUser");
        _dbContext.UserKycs.Add(kyc);
        await _dbContext.SaveChangesAsync(CancellationToken.None);

        var result = await _repository.GetByUserIdAsync(1, CancellationToken.None);

        result.Should().NotBeNull();
        result!.UserId.Should().Be(1);
        result.IdType.Should().Be(KycIdType.NationalIdCard);
        result.Nin.Should().Be("12345678901");
    }

    [Fact]
    public async Task AddAsync_SavesEntity()
    {
        var user = await CreateUserAsync(1);
        var kyc = new UserKyc
        {
            UserId = user.Id,
            IdType = KycIdType.InternationalPassport,
            GovernmentIdDocumentPathOrKey = "path/to/doc"
        };

        await _repository.AddAsync(kyc, CancellationToken.None);
        await _dbContext.SaveChangesAsync(CancellationToken.None);

        var saved = await _dbContext.UserKycs.FirstOrDefaultAsync(e => e.UserId == 1);
        saved.Should().NotBeNull();
        saved!.IdType.Should().Be(KycIdType.InternationalPassport);
    }

    [Fact]
    public async Task UpdateAsync_UpdatesEntity()
    {
        var user = await CreateUserAsync(1);
        var kyc = new UserKyc { UserId = user.Id, IdType = KycIdType.NationalIdCard, Nin = "111" };
        kyc.Created("TestUser");
        _dbContext.UserKycs.Add(kyc);
        await _dbContext.SaveChangesAsync(CancellationToken.None);

        kyc.Nin = "222";
        await _repository.UpdateAsync(kyc, CancellationToken.None);
        await _dbContext.SaveChangesAsync(CancellationToken.None);

        var updated = await _dbContext.UserKycs.FindAsync(kyc.Id);
        updated.Should().NotBeNull();
        updated!.Nin.Should().Be("222");
    }

    [Fact]
    public async Task GetByUserIdAsync_DeletedEntity_ReturnsNull()
    {
        var user = await CreateUserAsync(1);
        var kyc = new UserKyc { UserId = user.Id, IdType = KycIdType.NationalIdCard };
        kyc.Created("TestUser");
        kyc.Deleted("TestUser");
        _dbContext.UserKycs.Add(kyc);
        await _dbContext.SaveChangesAsync(CancellationToken.None);

        var result = await _repository.GetByUserIdAsync(1, CancellationToken.None);

        result.Should().BeNull();
    }

    public void Dispose() => _dbContext.Dispose();
}
