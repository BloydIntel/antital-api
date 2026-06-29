using Antital.Application.Features.Investments.ConfirmInvestmentOrder;
using Antital.Domain.Enums;
using Antital.Domain.Models;
using Antital.Infrastructure;
using Antital.Infrastructure.Repositories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Antital.Test.Application.Features.Investments;

public class ConfirmInvestmentOrderServiceTests : IDisposable
{
    private readonly AntitalDBContext _context;
    private readonly ConfirmInvestmentOrderService _service;

    public ConfirmInvestmentOrderServiceTests()
    {
        var options = new DbContextOptionsBuilder<AntitalDBContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _context = new AntitalDBContext(options);
        _service = new ConfirmInvestmentOrderService(new InvestmentOrderRepository(_context));
    }

    [Fact]
    public async Task TryFulfillAsync_CreatesHoldingAndUpdatesFunding()
    {
        var order = await SeedPaidOrderAsync();

        var fulfilled = await _service.TryFulfillAsync(order, "Test");
        await _context.SaveChangesAsync();

        fulfilled.Should().BeTrue();
        order.InvestorHoldingId.Should().NotBeNull();

        var holding = await _context.InvestorHoldings.SingleAsync();
        holding.InvestedAmount.Should().Be(1000m);
        holding.UnitHolding.Should().Be(10);
        holding.CurrentValue.Should().Be(1000m);

        var funding = await _context.OfferingFundings.SingleAsync();
        funding.RaisedAmount.Should().Be(101_000m);
        funding.InvestorCount.Should().Be(26);
    }

    [Fact]
    public async Task TryFulfillAsync_IsIdempotentWhenHoldingAlreadyLinked()
    {
        var order = await SeedPaidOrderAsync();
        await _service.TryFulfillAsync(order, "Test");
        await _context.SaveChangesAsync();

        var fulfilledAgain = await _service.TryFulfillAsync(order, "Test");
        await _context.SaveChangesAsync();

        fulfilledAgain.Should().BeTrue();
        (await _context.InvestorHoldings.CountAsync()).Should().Be(1);
        (await _context.OfferingFundings.SingleAsync()).RaisedAmount.Should().Be(101_000m);
    }

    private async Task<InvestmentOrder> SeedPaidOrderAsync()
    {
        var user = new User
        {
            Email = "fulfill@test.com",
            PasswordHash = "hash",
            UserType = UserTypeEnum.IndividualInvestor,
            IsEmailVerified = true,
            HasAgreedToTerms = true,
        };
        user.Created("Test");
        _context.Users.Add(user);

        var offering = new InvestmentOffering
        {
            Slug = "fulfill-co",
            Name = "Fulfill Co",
            Category = "Tech",
            Tagline = "Tagline",
            CoverImageUrl = "/img.jpg",
            RiskLevel = OfferingRiskLevel.Low,
            Status = OfferingStatus.Published,
            Funding = new OfferingFunding
            {
                RaisedAmount = 100_000m,
                FundingGoal = 500_000m,
                InvestorCount = 25,
                SharePrice = 100m,
                MinInvestment = 1000m,
                MaxInvestment = 50_000m,
            },
        };
        offering.Created("Test");
        offering.Funding.Created("Test");
        _context.InvestmentOfferings.Add(offering);
        await _context.SaveChangesAsync();

        var order = new InvestmentOrder
        {
            UserId = user.Id,
            OfferingId = offering.Id,
            Units = 10,
            SharePrice = 100m,
            Subtotal = 1000m,
            PlatformFeePercent = 2.5m,
            PlatformFee = 25m,
            TotalAmount = 1025m,
            Currency = "NGN",
            Status = InvestmentOrderStatus.Paid,
            PaystackReference = "ANT-ORD-1-abc",
            PaidAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddMinutes(30),
        };
        order.Created("Test");
        _context.InvestmentOrders.Add(order);
        await _context.SaveChangesAsync();
        return order;
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
