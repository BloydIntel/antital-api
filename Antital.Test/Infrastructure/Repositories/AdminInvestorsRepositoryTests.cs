using Antital.Domain.Enums;
using Antital.Domain.Models;
using Antital.Domain.Interfaces;
using Antital.Infrastructure;
using Antital.Infrastructure.Repositories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Antital.Test.Infrastructure.Repositories;

public sealed class AdminInvestorsRepositoryTests : IDisposable
{
    private readonly AntitalDBContext _context;
    private readonly AdminInvestorsRepository _repository;
    public AdminInvestorsRepositoryTests()
    {
        _context = new AntitalDBContext(new DbContextOptionsBuilder<AntitalDBContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        _repository = new AdminInvestorsRepository(_context);
    }

    [Fact]
    public async Task ListAsync_ReturnsSummaryAndFiltersInvestors()
    {
        var active = AddUser("active@example.com", InvestorAccountStatus.Active);
        var suspended = AddUser("suspended@example.com", InvestorAccountStatus.Suspended);
        _context.UserKycs.Add(new UserKyc { UserId = active.Id, ReviewStatus = InvestorKycStatus.Pending });
        _context.InvestorWallets.Add(new InvestorWallet { UserId = active.Id, AvailableBalance = 2_000_000 });
        _context.InvestorWallets.Add(new InvestorWallet { UserId = suspended.Id, AvailableBalance = 20 });
        await _context.SaveChangesAsync();
        var result = await _repository.ListAsync(new AdminInvestorQueryOptions("Active", "Pending", true, null, null, null, "wallet", true, 1, 10));
        result.TotalInvestors.Should().Be(2);
        result.PendingKyc.Should().Be(1);
        result.SuspendedAccounts.Should().Be(1);
        result.Items.Should().ContainSingle(x => x.Email == "active@example.com");
    }

    [Fact]
    public async Task UpdateAsync_UpdatesKycAndAccountStatus()
    {
        var user = AddUser("update@example.com", InvestorAccountStatus.Active);
        await _context.SaveChangesAsync();
        var updated = await _repository.UpdateAsync($"INV-{user.Id:0000}", new AdminInvestorMutation("Approved", "Reviewed", true), "admin");
        updated.Should().NotBeNull();
        updated!.AccountStatus.Should().Be(InvestorAccountStatus.Suspended);
        updated.KycStatus.Should().Be(InvestorKycStatus.Approved);
        updated.KycReviewNote.Should().Be("Reviewed");
    }

    [Fact]
    public async Task UpdateAsync_SuspensionActionPersistsReviewHistoryAndEvidence()
    {
        var user = AddUser("suspended-action@example.com", InvestorAccountStatus.Suspended);
        await _context.SaveChangesAsync();

        var updated = await _repository.UpdateAsync($"INV-{user.Id:0000}", new AdminInvestorMutation(null, "Submitted suspicious transaction report", true, "str", "request-1", "[{\"url\":\"evidence.pdf\"}]"), "admin");

        updated.Should().NotBeNull();
        updated!.SuspensionReview.StrFiled.Should().BeTrue();
        updated.SuspensionReview.Evidence.Should().Contain("evidence.pdf");
        _context.AdminInvestorSuspensionActions.Should().ContainSingle(x => x.RequestId == "request-1" && x.Action == "str");
    }

    [Fact]
    public async Task UpdateAsync_WithDuplicateRequestId_IsIdempotent()
    {
        var user = AddUser("idempotent-action@example.com", InvestorAccountStatus.Suspended);
        await _context.SaveChangesAsync();
        var mutation = new AdminInvestorMutation(null, "Contacted investor", true, "contact", "request-duplicate");

        await _repository.UpdateAsync($"INV-{user.Id:0000}", mutation, "admin");
        var updated = await _repository.UpdateAsync($"INV-{user.Id:0000}", mutation, "admin");

        updated.Should().NotBeNull();
        _context.AdminInvestorSuspensionActions.Count(x => x.RequestId == "request-duplicate").Should().Be(1);
    }

    private User AddUser(string email, InvestorAccountStatus status)
    {
        var user = new User { Email = email, PasswordHash = "test", UserType = UserTypeEnum.IndividualInvestor, FirstName = "Test", LastName = "Investor", PhoneNumber = "123", DateOfBirth = new DateTime(1990, 1, 1), CountryOfResidence = "Nigeria", StateOfResidence = "Lagos", Nationality = "Nigerian", ResidentialAddress = "Address", AccountStatus = status };
        user.Created("test"); _context.Users.Add(user); return user;
    }
    public void Dispose() => _context.Dispose();
}
