using DbTraffic.Core.Entities;
using DbTraffic.Infrastructure.Data;
using DbTraffic.Infrastructure.Repositories;

namespace DbTraffic.Infrastructure.Tests;

[Collection("SqlServer collection")]
public class InstanceSnapshotRepositoryTests
{
    private readonly SqlServerTestFixture _fixture;

    public InstanceSnapshotRepositoryTests(SqlServerTestFixture fixture)
    {
        _fixture = fixture;
    }

    private IDbConnectionFactory CreateFactory() => _fixture.CreateConnectionFactory();

    [Fact]
    public async Task CreateAndGetLatest_Should_PersistAndReturnLatestSnapshot()
    {
        if (!_fixture.IsAvailable)
        {
            return;
        }

        var instanceRepository = new InstanceRepository(CreateFactory());
        var instance = await instanceRepository.CreateAsync(new Instance
        {
            Name = $"Test Instance {Guid.NewGuid():N}",
            ConnectionString = _fixture.ConnectionString
        });

        var repository = new InstanceSnapshotRepository(CreateFactory());
        var snapshot = new InstanceSnapshot
        {
            InstanceId = instance.Id,
            CpuPercent = 25.5m,
            MemoryPercent = 60.0m,
            ActiveRequests = 5,
            BlockingSessions = 1,
            WaitTimeMs = 100,
            SnapshotJson = "{}"
        };

        await repository.CreateAsync(snapshot);

        var latest = await repository.GetLatestByInstanceIdAsync(instance.Id);

        Assert.NotNull(latest);
        Assert.Equal(snapshot.CpuPercent, latest.CpuPercent);
        Assert.Equal(snapshot.ActiveRequests, latest.ActiveRequests);
    }

    [Fact]
    public async Task GetByInstanceId_Should_LimitResults()
    {
        if (!_fixture.IsAvailable)
        {
            return;
        }

        var instanceRepository = new InstanceRepository(CreateFactory());
        var instance = await instanceRepository.CreateAsync(new Instance
        {
            Name = $"Test Instance {Guid.NewGuid():N}",
            ConnectionString = _fixture.ConnectionString
        });

        var repository = new InstanceSnapshotRepository(CreateFactory());
        for (var i = 0; i < 5; i++)
        {
            await repository.CreateAsync(new InstanceSnapshot
            {
                InstanceId = instance.Id,
                ActiveRequests = i,
                CapturedAt = DateTime.UtcNow.AddMinutes(i)
            });
        }

        var result = await repository.GetByInstanceIdAsync(instance.Id, 2);

        Assert.Equal(2, result.Count);
    }
}
