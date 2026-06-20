using DbTraffic.Core.Entities;
using DbTraffic.Infrastructure.Data;
using DbTraffic.Infrastructure.Discovery;
using DbTraffic.Infrastructure.Repositories;
using Microsoft.Extensions.Logging.Abstractions;

namespace DbTraffic.Infrastructure.Tests;

[Collection("SqlServer collection")]
public class DiscoveryServiceTests
{
    private readonly SqlServerTestFixture _fixture;

    public DiscoveryServiceTests(SqlServerTestFixture fixture)
    {
        _fixture = fixture;
    }

    private IDbConnectionFactory CreateFactory() => _fixture.CreateConnectionFactory();

    [Fact]
    public async Task DiscoverAllActiveInstancesAsync_Continues_When_One_Instance_Fails()
    {
        if (!_fixture.IsAvailable)
        {
            return;
        }

        var instanceRepository = new InstanceRepository(CreateFactory());
        var discoveryRepository = new DiscoveryRepository(CreateFactory());
        var service = new DiscoveryService(
            instanceRepository,
            discoveryRepository,
            NullLogger<DiscoveryService>.Instance);

        var healthyInstance = await instanceRepository.CreateAsync(new Instance
        {
            Name = "Healthy Instance",
            ConnectionString = _fixture.ConnectionString
        });

        var faultyInstance = await instanceRepository.CreateAsync(new Instance
        {
            Name = "Faulty Instance",
            ConnectionString = "Server=invalid;Database=master;User Id=x;Password=y;TrustServerCertificate=True;Connect Timeout=1"
        });

        var exception = await Record.ExceptionAsync(() => service.DiscoverAllActiveInstancesAsync());

        Assert.Null(exception);

        var healthyJobs = await discoveryRepository.GetJobsByInstanceAsync(healthyInstance.Id);
        Assert.NotNull(healthyJobs);

        var healthyObjects = await discoveryRepository.GetObjectsByInstanceAsync(healthyInstance.Id);
        Assert.NotNull(healthyObjects);
    }
}
