using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using DbTraffic.Shared.Models;

namespace DbTraffic.Web.Tests;

public class DashboardTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public DashboardTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Summary_Endpoint_Returns_Dashboard_Data()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/dashboard/summary");

        response.EnsureSuccessStatusCode();
        var summary = await response.Content.ReadFromJsonAsync<DashboardSummary>();

        Assert.NotNull(summary);
        Assert.True(summary.TotalInstances >= 0);
        Assert.True(summary.TotalProcesses >= 0);
        Assert.True(summary.TotalExecutions >= 0);
        Assert.True(summary.SuccessRate >= 0);
        Assert.True(summary.FailureRate >= 0);
    }
}
