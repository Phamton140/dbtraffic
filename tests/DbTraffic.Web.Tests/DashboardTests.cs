using System.Net.Http.Json;
using DbTraffic.Shared.Models;

namespace DbTraffic.Web.Tests;

[Collection("Web integration collection")]
public class DashboardTests
{
    private readonly WebApplicationFactoryFixture _factory;

    public DashboardTests(WebApplicationFactoryFixture factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Summary_Endpoint_Returns_Dashboard_Data()
    {
        if (!_factory.IsAvailable)
        {
            return;
        }

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
