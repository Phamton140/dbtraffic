using DbTraffic.Infrastructure.SqlServer;
using Microsoft.Extensions.Logging.Abstractions;

namespace DbTraffic.Infrastructure.Tests;

[Collection("SqlServer collection")]
public class SqlServerInstanceClientTests
{
    private readonly SqlServerTestFixture _fixture;

    public SqlServerInstanceClientTests(SqlServerTestFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task GetInstanceMetricsAsync_Returns_Metrics_Without_Throwing()
    {
        if (!_fixture.IsAvailable)
        {
            return;
        }

        await using var client = new SqlServerInstanceClient(_fixture.ConnectionString, NullLogger<SqlServerInstanceClient>.Instance);

        var metrics = await client.GetInstanceMetricsAsync();

        Assert.NotNull(metrics);
        Assert.True(metrics.ActiveRequests >= 0);
        Assert.True(metrics.BlockingSessions >= 0);
        Assert.True(metrics.WaitTimeMs >= 0);
        Assert.True(metrics.CpuPercent >= 0);
        Assert.True(metrics.MemoryPercent >= 0);
        Assert.NotNull(metrics.ActiveRequestsDetail);
    }

    [Fact]
    public async Task CanConnectAsync_Returns_False_For_Invalid_Connection_String_Without_Throwing()
    {
        await using var client = new SqlServerInstanceClient("Server=invalid;Database=master;User Id=x;Password=y;TrustServerCertificate=True;Connect Timeout=1", NullLogger<SqlServerInstanceClient>.Instance);

        var canConnect = await client.CanConnectAsync();

        Assert.False(canConnect);
    }
}
