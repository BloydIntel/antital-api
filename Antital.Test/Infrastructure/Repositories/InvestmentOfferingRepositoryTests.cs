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

public class InvestmentOfferingRepositoryTests : IDisposable
{
    private readonly AntitalDBContext _dbContext;
    private readonly InvestmentOfferingRepository _repository;

    public InvestmentOfferingRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<AntitalDBContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _dbContext = new AntitalDBContext(options);
        var currentUser = new Mock<ICurrentUser>();
        currentUser.Setup(x => x.UserName).Returns("TestUser");
        _repository = new InvestmentOfferingRepository(_dbContext, currentUser.Object);
    }

    [Fact]
    public async Task ListPublishedAsync_ReturnsOnlyPublishedOfferings()
    {
        await SeedOfferingAsync("published-co", OfferingStatus.Published);
        await SeedOfferingAsync("draft-co", OfferingStatus.Draft);

        var (items, total) = await _repository.ListPublishedAsync(1, 10, null, null, null, CancellationToken.None);

        total.Should().Be(1);
        items.Should().ContainSingle().Which.Slug.Should().Be("published-co");
    }

    [Fact]
    public async Task GetPublishedShellBySlugAsync_IncludesFundingAndDealTerms()
    {
        var offering = await SeedOfferingAsync("shell-co", OfferingStatus.Published);

        var result = await _repository.GetPublishedShellBySlugAsync("shell-co", CancellationToken.None);

        result.Should().NotBeNull();
        result!.Funding.Should().NotBeNull();
        result.DealTerms.Should().NotBeNull();
        result.Funding!.RaisedAmount.Should().Be(offering.Funding!.RaisedAmount);
    }

    [Fact]
    public async Task GetPublishedShellByIdOrSlugAsync_ResolvesNumericId()
    {
        var offering = await SeedOfferingAsync("by-id-co", OfferingStatus.Published);

        var result = await _repository.GetPublishedShellByIdOrSlugAsync(offering.Id.ToString(), CancellationToken.None);

        result.Should().NotBeNull();
        result!.Slug.Should().Be("by-id-co");
    }

    [Fact]
    public async Task GetHighlightsAsync_ReturnsOrderedHighlights()
    {
        var offering = await SeedOfferingAsync("highlights-co", OfferingStatus.Published);
        AddHighlight(offering.Id, HighlightKind.Stat, "₦1M ARR", "Revenue", 2);
        AddHighlight(offering.Id, HighlightKind.Bullet, null, "First bullet", 1);
        await _dbContext.SaveChangesAsync();

        var result = await _repository.GetHighlightsAsync(offering.Id, CancellationToken.None);

        result.Should().HaveCount(2);
        result[0].SortOrder.Should().Be(1);
        result[1].SortOrder.Should().Be(2);
    }

    [Fact]
    public async Task GetContentBlocksAsync_IncludesItems()
    {
        var offering = await SeedOfferingAsync("blocks-co", OfferingStatus.Published);
        var block = new OfferingContentBlock
        {
            OfferingId = offering.Id,
            BlockType = ContentBlockType.Narrative,
            Key = "edge",
            Title = "Edge",
            SortOrder = 1,
            Items =
            [
                new ContentBlockItem { Label = "Moat", Body = "Data advantage", SortOrder = 1 },
            ],
        };
        block.Created("TestUser");
        block.Items.First().Created("TestUser");
        _dbContext.OfferingContentBlocks.Add(block);
        await _dbContext.SaveChangesAsync();

        var result = await _repository.GetContentBlocksAsync(offering.Id, CancellationToken.None);

        result.Should().ContainSingle();
        result[0].Items.Should().ContainSingle().Which.Label.Should().Be("Moat");
    }

    private async Task<InvestmentOffering> SeedOfferingAsync(string slug, OfferingStatus status)
    {
        var offering = new InvestmentOffering
        {
            Slug = slug,
            Name = slug,
            Category = "Tech",
            Tagline = "Tagline",
            CoverImageUrl = "/img.jpg",
            RiskLevel = OfferingRiskLevel.Low,
            Status = status,
            PublishedAt = DateTime.UtcNow,
            Funding = new OfferingFunding
            {
                RaisedAmount = 1000,
                FundingGoal = 5000,
                InvestorCount = 10,
                SharePrice = 100,
                MinInvestment = 100,
                MaxInvestment = 1000,
            },
            DealTerms = new DealTerms
            {
                TotalSharesOffered = 1000,
                PricePerShare = 100,
                MinimumInvestment = 100,
                MaximumInvestment = 1000,
                MinimumThreshold = 500,
                FundingGoal = 5000,
                Deadline = DateTime.UtcNow.AddDays(30),
            },
        };
        offering.Created("TestUser");
        offering.Funding.Created("TestUser");
        offering.DealTerms.Created("TestUser");
        _dbContext.InvestmentOfferings.Add(offering);
        await _dbContext.SaveChangesAsync();
        return offering;
    }

    private void AddHighlight(int offeringId, HighlightKind kind, string? headline, string body, int sortOrder)
    {
        var highlight = new Highlight
        {
            OfferingId = offeringId,
            Kind = kind,
            Headline = headline,
            Body = body,
            SortOrder = sortOrder,
        };
        highlight.Created("TestUser");
        _dbContext.Highlights.Add(highlight);
    }

    public void Dispose()
    {
        _dbContext.Dispose();
    }
}
