using System.Text.RegularExpressions;
using Antital.Domain.Enums;
using Antital.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Antital.Infrastructure.Seed;

public static class InvestmentOfferingSeed
{
    private const string SeedUser = "system";

    public static async Task SeedAsync(AntitalDBContext context, ILogger logger, CancellationToken cancellationToken = default)
    {
        if (await context.InvestmentOfferings.AnyAsync(cancellationToken))
        {
            return;
        }

        var publishedAt = DateTime.UtcNow.AddDays(-30);
        var offerings = CreateListOfferings(publishedAt);
        context.InvestmentOfferings.AddRange(offerings);
        await context.SaveChangesAsync(cancellationToken);

        var greenTech = offerings.First(o => o.Slug == "greentech-solutions");
        SeedGreenTechDetail(context, greenTech, publishedAt);

        await context.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Seeded {Count} investment offerings.", offerings.Count);
    }

    private static List<InvestmentOffering> CreateListOfferings(DateTime publishedAt)
    {
        var cards = new[]
        {
            new ListCard("GreenTech Solutions", "Clean Energy", "Revolutionary solar panel technology with 40% higher efficiency", "/investments/ayka_solar.jpg", OfferingRiskLevel.Low, 234, 234, 1000m, 450000m, 1100000m),
            new ListCard("AgriTech Innovations", "Sustainable Farming", "High-yield crop technology, increasing production by 35%", "/investments/agri_tech.jpg", OfferingRiskLevel.Moderate, 567, 12, 5000m, 890000m, 1000000m),
            new ListCard("Solaris Innovations", "Agriculture", "Next-gen solar tech: 50% more efficient & weather-resilient", "/investments/solar_innovations.jpg", OfferingRiskLevel.Low, 876, 21, 5000m, 890000m, 1000000m),
            new ListCard("AquaPure Innovations", "Water Purification", "Next-gen filtration systems reducing contaminants by 90%", "/investments/aqua_pure.jpg", OfferingRiskLevel.Moderate, 150, 150, 500m, 300000m, 500000m),
            new ListCard("EcoBuild Materials", "Sustainable Construction", "Biodegradable materials for eco-friendly building solutions", "/investments/eco_build.jpg", OfferingRiskLevel.High, 120, 120, 2000m, 600000m, 800000m),
            new ListCard("SmartWaste Technologies", "Waste Management", "AI-powered sorting systems for efficient recycling", "/investments/smart_waste.jpg", OfferingRiskLevel.Low, 90, 90, 1500m, 250000m, 500000m),
            new ListCard("FinTech Innovators", "Finance", "Blockchain-based banking solutions for seamless transactions", "/investments/fintech_innovators.jpg", OfferingRiskLevel.High, 75, 1200, 12000m, 1500000m, 1800000m),
            new ListCard("TravelEasy Solutions", "Travel", "AI-powered travel itinerary planner for personalized experiences", "/investments/Travel_easy.jpg", OfferingRiskLevel.Low, 130, 700, 4000m, 350000m, 1000000m),
            new ListCard("SmartHome Technologies", "Home Automation", "Integrated home automation system for security and energy efficiency", "/investments/smart_home_technologies.jpg", OfferingRiskLevel.Moderate, 200, 600, 8000m, 900000m, 1500000m),
            new ListCard("EventTech Solutions", "Events", "Virtual reality event hosting platform for immersive experiences", "/investments/event_tech_solutions.jpg", OfferingRiskLevel.Moderate, 95, 1800, 6000m, 750000m, 1350000m),
            new ListCard("EcoFashion Brands", "Fashion", "Sustainable clothing line made from recycled materials", "/investments/eco_fashion_brands.jpg", OfferingRiskLevel.High, 160, 800, 3500m, 500000m, 1200000m),
            new ListCard("Wellness Apps", "Health & Wellness", "Mental health app with personalized resources and community support", "/investments/well_ness_apps.jpg", OfferingRiskLevel.Low, 100, 900, 2500m, 300000m, 1000000m),
        };

        return cards.Select(card =>
        {
            var offering = new InvestmentOffering
            {
                Slug = ToSlug(card.Name),
                Name = card.Name,
                Category = card.Category,
                Tagline = card.Description,
                CoverImageUrl = card.ImageUrl,
                RiskLevel = card.Risk,
                Status = OfferingStatus.Published,
                PublishedAt = publishedAt,
                Funding = new OfferingFunding
                {
                    RaisedAmount = card.Raised,
                    FundingGoal = card.Goal,
                    InvestorCount = card.Investors,
                    SharePrice = card.MinInvestment,
                    MinInvestment = card.MinInvestment,
                    MaxInvestment = card.MinInvestment * 50,
                },
                DealTerms = new DealTerms
                {
                    MinimumInvestment = card.MinInvestment,
                    MaximumInvestment = card.MinInvestment * 50,
                    MinimumThreshold = card.Goal * 0.5m,
                    FundingGoal = card.Goal,
                    PricePerShare = Math.Max(1, card.MinInvestment / 10),
                    TotalSharesOffered = (long)(card.Goal / Math.Max(1, card.MinInvestment / 10)),
                    Deadline = DateTime.UtcNow.AddDays(card.DaysLeft),
                },
            };

            offering.Created(SeedUser);
            offering.Funding.Created(SeedUser);
            offering.DealTerms.Created(SeedUser);
            return offering;
        }).ToList();
    }

    private static void SeedGreenTechDetail(AntitalDBContext context, InvestmentOffering offering, DateTime publishedAt)
    {
        offering.Funding!.RaisedAmount = 7_381_254m;
        offering.Funding.FundingGoal = 25_000_000m;
        offering.Funding.InvestorCount = 341;
        offering.Funding.SharePrice = 720m;
        offering.Funding.TargetRating = 4.5m;
        offering.Funding.MinInvestment = 5000m;
        offering.Funding.MaxInvestment = 250_000m;

        offering.DealTerms!.TotalSharesOffered = 99_431_817;
        offering.DealTerms.PricePerShare = 75m;
        offering.DealTerms.MinimumInvestment = 5000m;
        offering.DealTerms.MaximumInvestment = 250_000m;
        offering.DealTerms.MinimumThreshold = 15_000_000m;
        offering.DealTerms.FundingGoal = 25_000_000m;
        offering.DealTerms.Deadline = DateTime.UtcNow.AddDays(234);

        var corporateProfile = new OfferingCorporateProfile
        {
            OfferingId = offering.Id,
            EntityType = "C-Corp",
            Jurisdiction = "Nigeria",
            IncorporationYear = 2024,
            RegistrationId = "GTS-NG-800532",
            AdditionalNotes = "Incorporated in Lagos, Nigeria. Primary jurisdiction: Nigeria.",
        };
        corporateProfile.Created(SeedUser);
        context.OfferingCorporateProfiles.Add(corporateProfile);

        AddHighlights(context, offering.Id);
        AddContentBlocks(context, offering.Id);
        AddTeam(context, offering.Id);
        AddFinancials(context, offering.Id);
        AddRisks(context, offering.Id);
        AddDocuments(context, offering.Id);
        AddMedia(context, offering.Id);
        AddUpdates(context, offering.Id, publishedAt);
    }

    private static void AddHighlights(AntitalDBContext context, int offeringId)
    {
        var highlights = new (HighlightKind Kind, string? Headline, string Body, int Order)[]
        {
            (HighlightKind.Stat, "₦675M ARR", "Annual Recurring Revenue as of FY 2024", 1),
            (HighlightKind.Stat, "50+ Clients", "Active clients across West African markets", 2),
            (HighlightKind.Bullet, null, "Lifetime revenue of ₦2.1B across all clients", 3),
            (HighlightKind.Bullet, null, "Average monthly revenue per client: ₦11.25M", 4),
            (HighlightKind.Bullet, null, "Annual growth rate of 150% year-over-year", 5),
            (HighlightKind.Bullet, null, "Customer acquisition cost (CAC) of ₦4.5M", 6),
            (HighlightKind.Bullet, null, "Projected ARR for FY 2025: ₦2.25B", 7),
        };

        foreach (var h in highlights)
        {
            var entity = new Highlight
            {
                OfferingId = offeringId,
                Kind = h.Kind,
                Headline = h.Headline,
                Body = h.Body,
                SortOrder = h.Order,
            };
            entity.Created(SeedUser);
            context.Highlights.Add(entity);
        }
    }

    private static void AddContentBlocks(AntitalDBContext context, int offeringId)
    {
        var problem = new OfferingContentBlock
        {
            OfferingId = offeringId,
            BlockType = ContentBlockType.ProblemStatement,
            Title = "The Problem We Solve: Addressing Market Inefficiency",
            Summary = "Current small and medium-sized enterprises (SMEs) struggle with a complex, fragmented supply chain management system, leading to an average 15% loss in operational efficiency and increased waste. GreenTech Solutions is changing this with accessible, cloud-based solar intelligence.",
            SortOrder = 1,
        };
        problem.Created(SeedUser);
        context.OfferingContentBlocks.Add(problem);

        var proprietaryEdge = new OfferingContentBlock
        {
            OfferingId = offeringId,
            BlockType = ContentBlockType.Narrative,
            Key = "proprietary-edge",
            Title = "Our Proprietary Edge: Technology & Scalability",
            Summary = "At the core of GreenTech Solutions is a patent-pending solar efficiency engine that processes real-time irradiance and weather data to optimize panel output with 98.5% forecast accuracy.",
            SortOrder = 2,
            Items =
            [
                CreateBlockItem("Scalability", "Our platform uses modular microservices architecture, allowing expansion into new sectors with minimal friction.", 1),
                CreateBlockItem("Defensible Moat", "Three years of pilot data across 50 companies improves model accuracy with every deployment.", 2),
            ],
        };
        proprietaryEdge.Created(SeedUser);
        context.OfferingContentBlocks.Add(proprietaryEdge);

        var marketTraction = new OfferingContentBlock
        {
            OfferingId = offeringId,
            BlockType = ContentBlockType.Narrative,
            Key = "market-traction",
            Title = "Strong Market Traction & Financials",
            Summary = "GreenTech Solutions has demonstrated exceptional market traction with consistent revenue growth and expanding market presence.",
            SortOrder = 3,
            Items =
            [
                CreateBlockItem("Revenue Growth", "150% year-over-year revenue growth with 95% client retention.", 1),
                CreateBlockItem("Market Expansion", "Operating in three West African markets with 120+ qualified leads in pipeline.", 2),
            ],
        };
        marketTraction.Created(SeedUser);
        context.OfferingContentBlocks.Add(marketTraction);

        var useOfProceedsIntro = new OfferingContentBlock
        {
            OfferingId = offeringId,
            BlockType = ContentBlockType.Narrative,
            Key = "use-of-proceeds-intro",
            Title = "Use of Proceeds",
            Summary = "This section details how the capital being raised will be strategically allocated to achieve product and market acceleration.",
            SortOrder = 4,
        };
        useOfProceedsIntro.Created(SeedUser);
        context.OfferingContentBlocks.Add(useOfProceedsIntro);

        var tldr = new OfferingContentBlock
        {
            OfferingId = offeringId,
            BlockType = ContentBlockType.Tldr,
            Summary = "GreenTech Solutions transforms solar deployment for SMEs through advanced efficiency optimization, improving output while minimizing waste and operational cost.",
            SortOrder = 5,
        };
        tldr.Created(SeedUser);
        context.OfferingContentBlocks.Add(tldr);
    }

    private static ContentBlockItem CreateBlockItem(string label, string body, int order)
    {
        var item = new ContentBlockItem { Label = label, Body = body, SortOrder = order };
        item.Created(SeedUser);
        return item;
    }

    private static void AddTeam(AntitalDBContext context, int offeringId)
    {
        var members = new (string Name, string Title, string Bio, string Image, int Order)[]
        {
            ("Dr. Eleanor Vance", "CEO & Co-founder", "20 years in global logistics and clean energy optimization. Former VP of Operations at TransGlobal Freight. Ph.D. in Operations Research from MIT.", "/avatars/dr_eleanor.jpg", 1),
            ("Alex Chen", "CTO & Co-founder", "15 years building scalable machine learning platforms. Architect of the core efficiency engine. M.S. in Computer Science.", "/avatars/alex_chen.jpg", 2),
        };

        foreach (var m in members)
        {
            var entity = new TeamMember
            {
                OfferingId = offeringId,
                Name = m.Name,
                Title = m.Title,
                Bio = m.Bio,
                ImageUrl = m.Image,
                SortOrder = m.Order,
            };
            entity.Created(SeedUser);
            context.TeamMembers.Add(entity);
        }
    }

    private static void AddFinancials(AntitalDBContext context, int offeringId)
    {
        var metrics = new (string Name, string Period, int PeriodOrder, decimal? Value, FinancialMetricUnit Unit, FinancialValueType ValueType)[]
        {
            ("Annual Recurring Revenue (ARR)", "FY 2024 (Actual)", 1, 675_000_000m, FinancialMetricUnit.Currency, FinancialValueType.Actual),
            ("Annual Recurring Revenue (ARR)", "FY 2025 (Projected)", 2, 2_250_000_000m, FinancialMetricUnit.Currency, FinancialValueType.Projected),
            ("Annual Recurring Revenue (ARR)", "FY 2027 (Projected)", 3, 15_750_000_000m, FinancialMetricUnit.Currency, FinancialValueType.Projected),
            ("Gross Margin (%)", "FY 2024 (Actual)", 1, 78m, FinancialMetricUnit.Percent, FinancialValueType.Actual),
            ("Gross Margin (%)", "FY 2025 (Projected)", 2, 82m, FinancialMetricUnit.Percent, FinancialValueType.Projected),
            ("Gross Margin (%)", "FY 2027 (Projected)", 3, 86m, FinancialMetricUnit.Percent, FinancialValueType.Projected),
            ("Customer Acquisition Cost (CAC)", "FY 2024 (Actual)", 1, 4_500_000m, FinancialMetricUnit.Currency, FinancialValueType.Actual),
            ("Customer Acquisition Cost (CAC)", "FY 2025 (Projected)", 2, 3_750_000m, FinancialMetricUnit.Currency, FinancialValueType.Projected),
            ("Customer Acquisition Cost (CAC)", "FY 2027 (Projected)", 3, 2_250_000m, FinancialMetricUnit.Currency, FinancialValueType.Projected),
            ("Cash-Flow Positive Target", "FY 2024 (Actual)", 1, null, FinancialMetricUnit.Text, FinancialValueType.Actual),
            ("Cash-Flow Positive Target", "FY 2025 (Projected)", 2, null, FinancialMetricUnit.Text, FinancialValueType.Projected),
            ("Cash-Flow Positive Target", "FY 2027 (Projected)", 3, null, FinancialMetricUnit.Text, FinancialValueType.Projected),
        };

        foreach (var m in metrics)
        {
            var entity = new FinancialMetric
            {
                OfferingId = offeringId,
                MetricName = m.Name,
                PeriodLabel = m.Period,
                PeriodSortOrder = m.PeriodOrder,
                Value = m.Value,
                Unit = m.Unit,
                CurrencyCode = m.Unit == FinancialMetricUnit.Currency ? "NGN" : null,
                ValueType = m.ValueType,
            };
            entity.Created(SeedUser);
            context.FinancialMetrics.Add(entity);
        }

        var proceeds = new (decimal Percent, string Category, string Description, int Order)[]
        {
            (50m, "R&D", "Scaling the engineering team and accelerating next-gen panel software.", 1),
            (30m, "Sales & Marketing", "Expanding into Kenya and South Africa with dedicated sales hires.", 2),
            (20m, "Operations & Working Capital", "Securing key data licenses and general operational expenses.", 3),
        };

        foreach (var p in proceeds)
        {
            var entity = new UseOfProceedsItem
            {
                OfferingId = offeringId,
                AllocationPercent = p.Percent,
                Category = p.Category,
                Description = p.Description,
                SortOrder = p.Order,
            };
            entity.Created(SeedUser);
            context.UseOfProceedsItems.Add(entity);
        }
    }

    private static void AddRisks(AntitalDBContext context, int offeringId)
    {
        var risks = new (string Category, string Description, string Mitigation, int Order)[]
        {
            ("Market Risk", "Larger competitors could develop similar solar optimization capabilities.", "Focus on underserved SME segment and maintain rapid release cycles with defensible IP.", 1),
            ("Technology Risk", "Model accuracy relies on uninterrupted external weather data feeds.", "Diversified data ingestion pipeline with redundant data lake backup.", 2),
            ("Regulatory Risk", "Cross-border data privacy laws may require platform changes.", "Compliance-first modular architecture with ongoing legal review.", 3),
        };

        foreach (var r in risks)
        {
            var entity = new OfferingRisk
            {
                OfferingId = offeringId,
                Category = r.Category,
                Description = r.Description,
                Mitigation = r.Mitigation,
                SortOrder = r.Order,
            };
            entity.Created(SeedUser);
            context.OfferingRisks.Add(entity);
        }
    }

    private static void AddDocuments(AntitalDBContext context, int offeringId)
    {
        var doc = new OfferingDocument
        {
            OfferingId = offeringId,
            Title = "Official Prospectus & Financial Model",
            FileUrl = "/documents/greentech-prospectus.pdf",
            DocumentType = DocumentType.Prospectus,
            PageCount = 45,
        };
        doc.Created(SeedUser);
        context.OfferingDocuments.Add(doc);
    }

    private static void AddMedia(AntitalDBContext context, int offeringId)
    {
        var assets = new (MediaAssetType Type, string Url, int Order)[]
        {
            (MediaAssetType.Thumbnail, "/investments/thumb1.jpg", 1),
            (MediaAssetType.Thumbnail, "/investments/thumb2.jpg", 2),
            (MediaAssetType.Thumbnail, "/investments/thumb3.jpg", 3),
        };

        foreach (var a in assets)
        {
            var entity = new MediaAsset
            {
                OfferingId = offeringId,
                AssetType = a.Type,
                Url = a.Url,
                SortOrder = a.Order,
            };
            entity.Created(SeedUser);
            context.MediaAssets.Add(entity);
        }
    }

    private static void AddUpdates(AntitalDBContext context, int offeringId, DateTime publishedAt)
    {
        var updates = new (DateTime At, string Title, string Body, int Likes)[]
        {
            (DateTime.UtcNow, "75% Goal Reached!", "Thank you to our investors who have joined us. Founder AMA this Friday.", 45),
            (publishedAt.AddDays(-26), "Efficiency Engine V3.0 is Live", "Predictive rerouting reduces client delay costs by an average of 18%.", 90),
            (publishedAt.AddDays(-33), "Major Client Acquisition", "Signed a 3-year contract validating cold-chain expansion. ARR impact: +₦120M.", 2),
        };

        foreach (var u in updates)
        {
            var entity = new OfferingUpdate
            {
                OfferingId = offeringId,
                Status = OfferingUpdateStatus.Published,
                PublishedAt = u.At,
                Title = u.Title,
                Body = u.Body,
                LikeCount = u.Likes,
            };
            entity.Created(SeedUser);
            context.OfferingUpdates.Add(entity);
        }
    }

    private static string ToSlug(string name) =>
        Regex.Replace(name.ToLowerInvariant(), @"[^a-z0-9]+", "-").Trim('-');

    private sealed record ListCard(
        string Name,
        string Category,
        string Description,
        string ImageUrl,
        OfferingRiskLevel Risk,
        int Investors,
        int DaysLeft,
        decimal MinInvestment,
        decimal Raised,
        decimal Goal);
}
