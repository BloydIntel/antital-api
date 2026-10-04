using Antital.Domain.Enums;
using Antital.Domain.Interfaces;
using Antital.Domain.Models;
using Antital.Infrastructure;
using Antital.Infrastructure.Repositories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Antital.Test.Infrastructure.Repositories;

public sealed class AdminAlertsRepositoryTests : IDisposable
{
    private readonly AntitalDBContext _context;
    private readonly AdminAlertsRepository _repository;

    public AdminAlertsRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<AntitalDBContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _context = new AntitalDBContext(options);
        _repository = new AdminAlertsRepository(_context);
    }

    [Fact]
    public async Task GetAsync_FiltersAndReturnsSummaryCounts()
    {
        Add("FLG-1", "AML/Fraud", AlertSeverity.Critical, AlertStatus.Open, "Blacklisted IP", DateTime.UtcNow.AddMinutes(-1));
        Add("FLG-2", "KYC", AlertSeverity.High, AlertStatus.Open, "Missing document", DateTime.UtcNow.AddMinutes(-2));
        Add("FLG-3", "AML/Fraud", AlertSeverity.Low, AlertStatus.Dismissed, "Resolved review", DateTime.UtcNow.AddMinutes(-3));
        Add("FLG-4", "AML/Fraud", AlertSeverity.Critical, AlertStatus.Resolved, "Closed review", DateTime.UtcNow.AddMinutes(-4));
        await _context.SaveChangesAsync();

        var result = await _repository.GetAsync(new AdminAlertsQueryOptions("AML/Fraud", null, null, null, 1, 10));

        result.TotalCount.Should().Be(3);
        result.Critical.Should().Be(1);
        result.Warnings.Should().Be(1);
        result.Items.Select(x => x.PublicId).Should().ContainInOrder("FLG-1", "FLG-3", "FLG-4");
    }

    [Fact]
    public async Task UpdateAsync_ChangesStateAndNotes_AndIgnoresMissingOrDeleted()
    {
        Add("FLG-5", "AML/Fraud", AlertSeverity.Medium, AlertStatus.Open, "Review", DateTime.UtcNow);
        var deleted = Add("FLG-6", "AML/Fraud", AlertSeverity.Medium, AlertStatus.Open, "Deleted", DateTime.UtcNow);
        deleted.Deleted("test");
        await _context.SaveChangesAsync();

        var updated = await _repository.UpdateAsync("FLG-5", new AdminAlertUpdate("Acknowledged", 42, "Reviewed by admin"), "admin", CancellationToken.None);

        updated.Should().NotBeNull();
        updated!.Status.Should().Be(AlertStatus.Acknowledged);
        updated.AssigneeUserId.Should().Be(42);
        updated.ResolutionNote.Should().Be("Reviewed by admin");
        (await _repository.UpdateAsync("FLG-6", new AdminAlertUpdate("Resolved", null, null), "admin")).Should().BeNull();
        (await _repository.GetByPublicIdAsync("missing")).Should().BeNull();
    }

    private PlatformAlert Add(string id, string type, AlertSeverity severity, AlertStatus status, string description, DateTime occurredAt)
    {
        var alert = new PlatformAlert { PublicId = id, Type = type, Severity = severity, Status = status, EntityAffected = "Test entity", Description = description, OccurredAtUtc = occurredAt };
        alert.Created("test");
        _context.PlatformAlerts.Add(alert);
        return alert;
    }

    public void Dispose() => _context.Dispose();
}
