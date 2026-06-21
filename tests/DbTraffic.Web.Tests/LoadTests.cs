using System.Diagnostics;
using Microsoft.AspNetCore.Mvc.Testing;

namespace DbTraffic.Web.Tests;

public class LoadTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public LoadTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Health_Endpoint_Handles_Concurrent_Requests()
    {
        const int requestCount = 100;
        const int maxAverageMilliseconds = 500;

        var client = _factory.CreateClient();
        var stopwatch = Stopwatch.StartNew();

        var tasks = Enumerable
            .Range(0, requestCount)
            .Select(_ => client.GetAsync("/api/health"));

        var responses = await Task.WhenAll(tasks);
        stopwatch.Stop();

        var averageMilliseconds = stopwatch.Elapsed.TotalMilliseconds / requestCount;

        Assert.All(responses, response => Assert.True(response.IsSuccessStatusCode));
        Assert.True(averageMilliseconds < maxAverageMilliseconds,
            $"Average response time {averageMilliseconds:F2}ms exceeded {maxAverageMilliseconds}ms");
    }
}
