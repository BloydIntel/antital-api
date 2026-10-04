using Antital.Domain.Enums;
using Antital.Domain.Interfaces;
using Antital.Domain.Models;
using Antital.Infrastructure;
using Antital.Infrastructure.Repositories;
using BuildingBlocks.Domain.Models;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Antital.Test.Infrastructure.Repositories;

public class AdminDashboardRepositoryTests : IDisposable
{
    private readonly AntitalDBContext _context;
    private readonly AdminDashboardRepository _repository;

    public AdminDashboardRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<AntitalDBContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _context = new AntitalDBContext(options);
        _repository = new AdminDashboardRepository(_context);
    }

    [Fact]
    public async Task EmptyDatabase_ReturnsZeroMetricsAndNoEvents()
    {
        var now = DateTime.UtcNow;

        var investors = await _repository.GetInvestorMetricsAsync(
            now.AddDays(-30),
            now,
            now.AddDays(-60),
            now.AddDays(-30));
        var campaigns = await _repository.GetCampaignMetricsAsync();
        var actions = await _repository.GetActionCountsAsync(now.AddDays(-30), now);
        var events = await _repository.GetRecentEventsAsync(5);

        investors.Should().Be(new AdminInvestorMetrics(0, 0, 0));
        campaigns.Should().Be(new AdminCampaignMetrics(0, 0m, 0, 0m));
        actions.Should().Be(new AdminActionCounts(0, 0, 0));
        events.Should().BeEmpty();
    }

    [Fact]
    public async Task GetInvestorMetrics_FiltersRolesTypesSoftDeletesAndPeriodBoundaries()
    {
        var now = new DateTime(2026, 10, 4, 18, 30, 0, DateTimeKind.Utc);
        AddUser("current-individual@example.com", UserTypeEnum.IndividualInvestor, UserRoleEnum.User, now.AddDays(-1));
        AddUser("current-corporate@example.com", UserTypeEnum.CorporateInvestor, UserRoleEnum.User, now.AddDays(-30));
        AddUser("previous@example.com", UserTypeEnum.IndividualInvestor, UserRoleEnum.User, now.AddDays(-31));
        AddUser("old@example.com", UserTypeEnum.CorporateInvestor, UserRoleEnum.User, now.AddDays(-90));
        AddUser("fundraiser@example.com", UserTypeEnum.FundRaiser, UserRoleEnum.User, now.AddDays(-1));
        AddUser("admin@example.com", UserTypeEnum.IndividualInvestor, UserRoleEnum.Admin, now.AddDays(-1));
        AddUser("deleted@example.com", UserTypeEnum.IndividualInvestor, UserRoleEnum.User, now.AddDays(-1), deleted: true);
        await _context.SaveChangesAsync();

        var metrics = await _repository.GetInvestorMetricsAsync(
            now.AddDays(-30),
            now,
            now.AddDays(-60),
            now.AddDays(-30));

        metrics.TotalInvestors.Should().Be(4);
        metrics.NewInvestorsInPeriod.Should().Be(2);
        metrics.NewInvestorsInPreviousPeriod.Should().Be(1);
    }

    [Fact]
    public async Task GetCampaignMetrics_UsesLiveFundingAndExcludesDeletedRecords()
    {
        AddOffering("published", OfferingStatus.Published, 250m);
        AddOffering("draft", OfferingStatus.Draft, 75m);
        AddOffering("closed", OfferingStatus.Closed, 100m);
        AddOffering("deleted", OfferingStatus.Published, 900m, deleted: true);
        AddOffering("deleted-funding", OfferingStatus.Published, 500m, fundingDeleted: true);
        await _context.SaveChangesAsync();

        var metrics = await _repository.GetCampaignMetricsAsync();

        metrics.TotalCampaigns.Should().Be(4);
        metrics.ActiveCampaigns.Should().Be(2);
        metrics.ActiveCampaignRaisedAmount.Should().Be(250m);
        metrics.TotalFundsRaised.Should().Be(425m);
    }

    [Fact]
    public async Task GetActionCounts_UsesReviewStatusesAndCurrentPeriodFailures()
    {
        var now = new DateTime(2026, 10, 4, 18, 30, 0, DateTimeKind.Utc);
        var submittedUser = AddUser("submitted@example.com", UserTypeEnum.IndividualInvestor, UserRoleEnum.User, now.AddDays(-5));
        var reviewUser = AddUser("review@example.com", UserTypeEnum.CorporateInvestor, UserRoleEnum.User, now.AddDays(-5));
        var activatedUser = AddUser("activated@example.com", UserTypeEnum.IndividualInvestor, UserRoleEnum.User, now.AddDays(-5));
        AddOnboarding(submittedUser, OnboardingStatus.Submitted, now.AddDays(-4));
        AddOnboarding(reviewUser, OnboardingStatus.UnderReview, now.AddDays(-3));
        AddOnboarding(activatedUser, OnboardingStatus.Activated, now.AddDays(-2));
        AddOffering("draft-action", OfferingStatus.Draft, 0m);
        AddOffering("published-action", OfferingStatus.Published, 0m);
        AddPaymentTransaction(PaymentTransactionStatus.Failed, now.AddDays(-40), now.AddDays(-1));
        AddPaymentTransaction(PaymentTransactionStatus.Failed, now.AddDays(-31));
        AddPaymentTransaction(PaymentTransactionStatus.Success, now.AddDays(-1));
        await _context.SaveChangesAsync();

        var counts = await _repository.GetActionCountsAsync(now.AddDays(-30), now);

        counts.Should().Be(new AdminActionCounts(2, 1, 1));
    }

    [Fact]
    public async Task GetRecentEvents_MergesOrdersAndLimitsNewestFirst()
    {
        var now = new DateTime(2026, 10, 4, 18, 30, 0, DateTimeKind.Utc);
        var investor = AddUser("activity@example.com", UserTypeEnum.IndividualInvestor, UserRoleEnum.User, now.AddHours(-4));
        AddOnboarding(investor, OnboardingStatus.Submitted, now.AddHours(-3));
        var offering = AddOffering("activity-campaign", OfferingStatus.Published, 300m, now.AddHours(-2));
        AddPaidOrder(investor, offering, now.AddHours(-1), 125_000m);
        await _context.SaveChangesAsync();

        var events = await _repository.GetRecentEventsAsync(3);

        events.Should().HaveCount(3);
        events.Select(item => item.Kind).Should().Equal(
            AdminDashboardEventKind.InvestmentCompleted,
            AdminDashboardEventKind.CampaignPublished,
            AdminDashboardEventKind.OnboardingSubmitted);
        events[0].Amount.Should().Be(125_000m);
        events[0].Currency.Should().Be("NGN");
        events.Should().BeInDescendingOrder(item => item.OccurredAtUtc);
    }

    [Fact]
    public async Task GetRecentEvents_NonPositiveLimit_ReturnsEmpty()
    {
        var events = await _repository.GetRecentEventsAsync(0);

        events.Should().BeEmpty();
    }

    private User AddUser(
        string email,
        UserTypeEnum userType,
        UserRoleEnum role,
        DateTime createdAt,
        bool deleted = false)
    {
        var user = new User
        {
            Email = email,
            PasswordHash = "hash",
            UserType = userType,
            Role = role,
            IsEmailVerified = true,
            FirstName = "Test",
            LastName = "User",
            PhoneNumber = "+2348000000000",
            DateOfBirth = new DateTime(1990, 1, 1),
            Nationality = "Nigerian",
            CountryOfResidence = "Nigeria",
            StateOfResidence = "Lagos",
            ResidentialAddress = "Test address",
            HasAgreedToTerms = true,
        };
        user.Created("test");
        if (deleted)
        {
            user.Deleted("test");
        }

        _context.Users.Add(user);
        SetCreatedAt(user, createdAt);
        return user;
    }

    private UserOnboarding AddOnboarding(User user, OnboardingStatus status, DateTime submittedAt)
    {
        var onboarding = new UserOnboarding
        {
            User = user,
            Status = status,
            SubmittedAt = submittedAt,
        };
        onboarding.Created("test");
        _context.UserOnboardings.Add(onboarding);
        SetCreatedAt(onboarding, submittedAt);
        return onboarding;
    }

    private InvestmentOffering AddOffering(
        string slug,
        OfferingStatus status,
        decimal raisedAmount,
        DateTime? publishedAt = null,
        bool deleted = false,
        bool fundingDeleted = false)
    {
        var funding = new OfferingFunding
        {
            RaisedAmount = raisedAmount,
            FundingGoal = 1_000m,
            InvestorCount = 1,
            SharePrice = 10m,
            MinInvestment = 10m,
            MaxInvestment = 1_000m,
        };
        funding.Created("test");
        if (fundingDeleted)
        {
            funding.Deleted("test");
        }

        var offering = new InvestmentOffering
        {
            Slug = slug,
            Name = slug,
            Category = "Test",
            Tagline = "Test",
            CoverImageUrl = "/test.png",
            Status = status,
            PublishedAt = publishedAt,
            Funding = funding,
        };
        offering.Created("test");
        if (deleted)
        {
            offering.Deleted("test");
        }

        _context.InvestmentOfferings.Add(offering);
        return offering;
    }

    private void AddPaymentTransaction(
        PaymentTransactionStatus status,
        DateTime createdAt,
        DateTime? processedAt = null)
    {
        var suffix = Guid.NewGuid().ToString("N");
        var user = AddUser(
            $"payment-{suffix}@example.com",
            UserTypeEnum.IndividualInvestor,
            UserRoleEnum.User,
            createdAt);
        var offering = AddOffering($"payment-{suffix}", OfferingStatus.Published, 0m);
        var order = new InvestmentOrder
        {
            User = user,
            Offering = offering,
            Units = 1,
            SharePrice = 10m,
            Subtotal = 10m,
            TotalAmount = 10m,
            Currency = "NGN",
            Status = status == PaymentTransactionStatus.Success
                ? InvestmentOrderStatus.Paid
                : InvestmentOrderStatus.Failed,
            PaidAt = status == PaymentTransactionStatus.Success ? processedAt ?? createdAt : null,
        };
        order.Created("test");
        _context.InvestmentOrders.Add(order);
        SetCreatedAt(order, createdAt);

        var transaction = new PaymentTransaction
        {
            Order = order,
            Provider = "Paystack",
            Reference = suffix,
            Status = status,
            ProcessedAt = processedAt,
        };
        transaction.Created("test");
        _context.PaymentTransactions.Add(transaction);
        SetCreatedAt(transaction, createdAt);
    }

    private void AddPaidOrder(User user, InvestmentOffering offering, DateTime paidAt, decimal amount)
    {
        var order = new InvestmentOrder
        {
            User = user,
            Offering = offering,
            Units = 10,
            SharePrice = amount / 10,
            Subtotal = amount,
            TotalAmount = amount,
            Currency = "NGN",
            Status = InvestmentOrderStatus.Paid,
            PaidAt = paidAt,
        };
        order.Created("test");
        _context.InvestmentOrders.Add(order);
        SetCreatedAt(order, paidAt);
    }

    private void SetCreatedAt(TrackableEntity entity, DateTime createdAt)
    {
        _context.Entry(entity).Property(nameof(TrackableEntity.CreatedAt)).CurrentValue = createdAt;
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
