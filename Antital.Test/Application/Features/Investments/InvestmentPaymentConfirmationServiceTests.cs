using Antital.Application.Features.Investments.ConfirmInvestmentOrder;
using Antital.Application.Features.Investments.ProcessPaystackWebhook;
using Antital.Domain.Enums;
using Antital.Domain.Interfaces;
using Antital.Domain.Models;
using Antital.Infrastructure;
using Antital.Infrastructure.Repositories;
using BuildingBlocks.Domain.Interfaces;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace Antital.Test.Application.Features.Investments;

public class InvestmentPaymentConfirmationServiceTests : IDisposable
{
    private readonly AntitalDBContext _context;
    private readonly InvestmentPaymentConfirmationService _service;

    public InvestmentPaymentConfirmationServiceTests()
    {
        var options = new DbContextOptionsBuilder<AntitalDBContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _context = new AntitalDBContext(options);

        var unitOfWork = new Mock<IAntitalUnitOfWork>();
        unitOfWork
            .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(async (CancellationToken ct) => await _context.SaveChangesAsync(ct) > 0);

        var orderRepository = new InvestmentOrderRepository(_context);
        _service = new InvestmentPaymentConfirmationService(
            orderRepository,
            new ConfirmInvestmentOrderService(orderRepository),
            unitOfWork.Object,
            new TestCurrentUser());
    }

    [Fact]
    public async Task TryConfirmSuccessfulChargeAsync_MarksOrderPaidAndStoresTransaction()
    {
        await SeedPendingOrderAsync(reference: "ANT-ORD-1-abc", totalAmount: 1025m);

        var handled = await _service.TryConfirmSuccessfulChargeAsync(
            "ANT-ORD-1-abc",
            102_500,
            "card",
            """{"event":"charge.success"}""");

        handled.Should().BeTrue();
        var updated = await _context.InvestmentOrders.SingleAsync();
        updated.Status.Should().Be(InvestmentOrderStatus.Paid);
        updated.PaidAt.Should().NotBeNull();
        updated.InvestorHoldingId.Should().NotBeNull();

        var transaction = await _context.PaymentTransactions.SingleAsync();
        transaction.Status.Should().Be(PaymentTransactionStatus.Success);
        transaction.Reference.Should().Be("ANT-ORD-1-abc");

        var funding = await _context.OfferingFundings.SingleAsync();
        funding.RaisedAmount.Should().Be(101_000m);
    }

    [Fact]
    public async Task TryConfirmSuccessfulChargeAsync_IsIdempotentWhenAlreadyPaid()
    {
        await SeedPendingOrderAsync(reference: "ANT-ORD-2-abc", totalAmount: 1025m);
        await _service.TryConfirmSuccessfulChargeAsync(
            "ANT-ORD-2-abc",
            102_500,
            "card",
            """{"event":"charge.success"}""");

        var handled = await _service.TryConfirmSuccessfulChargeAsync(
            "ANT-ORD-2-abc",
            102_500,
            "card",
            """{"event":"charge.success"}""");

        handled.Should().BeTrue();
        (await _context.PaymentTransactions.CountAsync()).Should().Be(1);
        (await _context.InvestorHoldings.CountAsync()).Should().Be(1);
    }

    private async Task SeedPendingOrderAsync(string reference, decimal totalAmount)
    {
        var user = new User
        {
            Email = "pay@test.com",
            PasswordHash = "hash",
            UserType = UserTypeEnum.IndividualInvestor,
            IsEmailVerified = true,
            HasAgreedToTerms = true,
        };
        user.Created("Test");
        _context.Users.Add(user);

        var offering = new InvestmentOffering
        {
            Slug = "pay-co",
            Name = "Pay Co",
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
            TotalAmount = totalAmount,
            Currency = "NGN",
            Status = InvestmentOrderStatus.PendingPayment,
            PaystackReference = reference,
            ExpiresAt = DateTime.UtcNow.AddMinutes(30),
        };
        order.Created("Test");
        _context.InvestmentOrders.Add(order);
        await _context.SaveChangesAsync();
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    private sealed class TestCurrentUser : ICurrentUser
    {
        public string UserName => "Test";
        public string IPAddress => "127.0.0.1";
    }
}
