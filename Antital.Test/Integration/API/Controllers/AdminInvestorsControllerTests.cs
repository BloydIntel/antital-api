using System.Net;
using Antital.Test.Integration;
using FluentAssertions;
using Xunit;

namespace Antital.Test.Integration.API.Controllers;

[Collection("IntegrationTests")]
public sealed class AdminInvestorsControllerTests : IClassFixture<CustomWebApplicationFactory<Program>>
{
    private readonly CustomWebApplicationFactory<Program> _factory;
    public AdminInvestorsControllerTests(CustomWebApplicationFactory<Program> factory) => _factory = factory;

    [Fact]
    public async Task List_WithoutAuthentication_Returns401()
    {
        using var client = _factory.CreateClient();
        (await client.GetAsync("/api/admin/investors")).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

}
