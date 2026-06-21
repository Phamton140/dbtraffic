using System.Diagnostics;

namespace DbTraffic.Web.Tests;

[Collection("Web integration collection")]
public class LoadTests
{
    private readonly WebApplicationFactoryFixture _factory;

    public LoadTests(WebApplicationFactoryFixture factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Health_Endpoint_Handles_Concurrent_Requests()
    {
        if (!_factory.IsAvailable)
        {
            return;
        }

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
