using System.Net;
using System.Net.Http.Json;
using Antital.Application.DTOs.Investments;
using Antital.Domain.Enums;
using Antital.Domain.Models;
using Antital.Infrastructure;
using Antital.Test.Integration;
using BuildingBlocks.Application.Features;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Antital.Test.Integration.API.Controllers;

[Collection("IntegrationTests")]
public class InvestmentsControllerTests : IClassFixture<CustomWebApplicationFactory<Program>>, IDisposable
{
    private readonly CustomWebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;
    private readonly IServiceScope _scope;
    private readonly AntitalDBContext _context;
    private static readonly System.Text.Json.JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
    };

    public InvestmentsControllerTests(CustomWebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
        _scope = _factory.Services.CreateScope();
        _context = _scope.ServiceProvider.GetRequiredService<AntitalDBContext>();
        CleanupDatabase();
    }

    [Fact]
    public async Task List_ReturnsPublishedOfferingsWithoutAuth()
    {
        await SeedOfferingAsync("alpha-co", OfferingRiskLevel.Low, OfferingStatus.Published, "Clean Energy");
        await SeedOfferingAsync("beta-co", OfferingRiskLevel.High, OfferingStatus.Draft, "Finance");

        var response = await _client.GetAsync("/api/investments");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<Result<InvestmentListResponse>>(JsonOptions);
        result!.IsSuccess.Should().BeTrue();
        result.Value!.Items.Should().ContainSingle(i => i.Slug == "alpha-co");
        result.Value.TotalCount.Should().Be(1);
    }

    [Fact]
    public async Task List_FiltersByRisk()
    {
        await SeedOfferingAsync("low-co", OfferingRiskLevel.Low, OfferingStatus.Published, "Tech");
        await SeedOfferingAsync("high-co", OfferingRiskLevel.High, OfferingStatus.Published, "Tech");

        var response = await _client.GetAsync("/api/investments?risk=low");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<Result<InvestmentListResponse>>(JsonOptions);
        result!.Value!.Items.Should().ContainSingle(i => i.Slug == "low-co");
    }

    [Fact]
    public async Task GetShell_BySlug_ReturnsOfferingFundingAndDealTerms()
    {
        var offering = await SeedOfferingAsync("shell-co", OfferingRiskLevel.Moderate, OfferingStatus.Published, "Agri");

        var response = await _client.GetAsync("/api/investments/shell-co");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<Result<OfferingShellResponse>>(JsonOptions);
        result!.Value!.Offering.Slug.Should().Be("shell-co");
        result.Value.Funding.RaisedAmount.Should().Be(offering.Funding!.RaisedAmount);
        result.Value.DealTerms.FundingGoal.Should().Be(offering.DealTerms!.FundingGoal);
    }

    [Fact]
    public async Task GetShell_ByNumericId_ReturnsOffering()
    {
        var offering = await SeedOfferingAsync("by-id-co", OfferingRiskLevel.Low, OfferingStatus.Published, "Tech");

        var response = await _client.GetAsync($"/api/investments/{offering.Id}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<Result<OfferingShellResponse>>(JsonOptions);
        result!.Value!.Offering.Id.Should().Be(offering.Id);
    }

    [Fact]
    public async Task GetShell_UnknownSlug_Returns404()
    {
        var response = await _client.GetAsync("/api/investments/does-not-exist");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetHighlights_ReturnsDomainCollection()
    {
        var offering = await SeedOfferingAsync("highlights-co", OfferingRiskLevel.Low, OfferingStatus.Published, "Tech");
        var highlight = new Highlight
        {
            OfferingId = offering.Id,
            Kind = HighlightKind.Stat,
            Headline = "₦1M ARR",
            Body = "Annual revenue",
            SortOrder = 1,
        };
        highlight.Created("TestUser");
        _context.Highlights.Add(highlight);
        await _context.SaveChangesAsync();

        var response = await _client.GetAsync("/api/investments/highlights-co/highlights");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<Result<List<HighlightDto>>>(JsonOptions);
        result!.Value!.Should().ContainSingle(h => h.Headline == "₦1M ARR");
    }

    [Fact]
    public async Task GetFinancials_ReturnsMetricsAndUseOfProceeds()
    {
        var offering = await SeedOfferingAsync("financials-co", OfferingRiskLevel.Low, OfferingStatus.Published, "Tech");
        var metric = new FinancialMetric
        {
            OfferingId = offering.Id,
            MetricName = "ARR",
            PeriodLabel = "FY 2024 (Actual)",
            PeriodSortOrder = 1,
            Value = 1000m,
            Unit = FinancialMetricUnit.Currency,
            CurrencyCode = "NGN",
            ValueType = FinancialValueType.Actual,
        };
        metric.Created("TestUser");
        var proceeds = new UseOfProceedsItem
        {
            OfferingId = offering.Id,
            AllocationPercent = 50m,
            Category = "R&D",
            Description = "Engineering",
            SortOrder = 1,
        };
        proceeds.Created("TestUser");
        _context.FinancialMetrics.Add(metric);
        _context.UseOfProceedsItems.Add(proceeds);
        await _context.SaveChangesAsync();

        var response = await _client.GetAsync("/api/investments/financials-co/financials");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<Result<OfferingFinancialsResponse>>(JsonOptions);
        result!.Value!.Metrics.Should().ContainSingle(m => m.MetricName == "ARR");
        result.Value.UseOfProceeds.Should().ContainSingle(p => p.Category == "R&D");
    }

    [Fact]
    public async Task GetContentBlocks_ReturnsBlocksWithItems()
    {
        var offering = await SeedOfferingAsync("blocks-co", OfferingRiskLevel.Low, OfferingStatus.Published, "Tech");
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
        _context.OfferingContentBlocks.Add(block);
        await _context.SaveChangesAsync();

        var response = await _client.GetAsync("/api/investments/blocks-co/content-blocks");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<Result<List<ContentBlockDto>>>(JsonOptions);
        result!.Value.Should().NotBeNull();
        result.Value!.Should().ContainSingle(b => b.Key == "edge");
        result.Value[0].Items.Should().ContainSingle(i => i.Label == "Moat");
    }

    private async Task<InvestmentOffering> SeedOfferingAsync(
        string slug,
        OfferingRiskLevel risk,
        OfferingStatus status,
        string category)
    {
        var offering = new InvestmentOffering
        {
            Slug = slug,
            Name = slug,
            Category = category,
            Tagline = "Tagline for " + slug,
            CoverImageUrl = "/img.jpg",
            RiskLevel = risk,
            Status = status,
            PublishedAt = DateTime.UtcNow,
            Funding = new OfferingFunding
            {
                RaisedAmount = 100_000m,
                FundingGoal = 500_000m,
                InvestorCount = 25,
                SharePrice = 100m,
                MinInvestment = 1000m,
                MaxInvestment = 50_000m,
            },
            DealTerms = new DealTerms
            {
                TotalSharesOffered = 10_000,
                PricePerShare = 100m,
                MinimumInvestment = 1000m,
                MaximumInvestment = 50_000m,
                MinimumThreshold = 250_000m,
                FundingGoal = 500_000m,
                Deadline = DateTime.UtcNow.AddDays(30),
            },
        };
        offering.Created("TestUser");
        offering.Funding.Created("TestUser");
        offering.DealTerms.Created("TestUser");
        _context.InvestmentOfferings.Add(offering);
        await _context.SaveChangesAsync();
        return offering;
    }

    private void CleanupDatabase()
    {
        _context.InvestmentOfferings.RemoveRange(_context.InvestmentOfferings.IgnoreQueryFilters());
        _context.SaveChanges();
    }

    public void Dispose()
    {
        CleanupDatabase();
        _scope.Dispose();
        _client.Dispose();
    }
}
