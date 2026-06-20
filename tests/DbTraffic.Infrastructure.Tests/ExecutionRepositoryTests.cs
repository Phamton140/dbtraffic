using DbTraffic.Core.Entities;
using DbTraffic.Infrastructure.Data;
using DbTraffic.Infrastructure.Repositories;

namespace DbTraffic.Infrastructure.Tests;

[Collection("SqlServer collection")]
public class ExecutionRepositoryTests
{
    private readonly SqlServerTestFixture _fixture;

    public ExecutionRepositoryTests(SqlServerTestFixture fixture)
    {
        _fixture = fixture;
    }

    private IDbConnectionFactory CreateFactory() => _fixture.CreateConnectionFactory();

    [Fact]
    public async Task CreateAndGet_Should_PersistAndReturnExecution()
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

        var repository = new ExecutionRepository(CreateFactory());
        var execution = new Execution
        {
            InstanceId = instance.Id,
            Source = "Manual",
            StartedAt = DateTime.UtcNow,
            Status = "Completed",
            DurationMinutes = 15,
            Notes = "Test execution"
        };

        await repository.CreateAsync(execution);

        var result = await repository.GetByIdAsync(execution.Id);

        Assert.NotNull(result);
        Assert.Equal(execution.Status, result.Status);
        Assert.Equal(execution.DurationMinutes, result.DurationMinutes);
        Assert.Equal(execution.Notes, result.Notes);
    }

    [Fact]
    public async Task GetByInstanceId_Should_ReturnExecutionsForInstance()
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

        var repository = new ExecutionRepository(CreateFactory());
        await repository.CreateAsync(new Execution
        {
            InstanceId = instance.Id,
            Source = "Manual",
            StartedAt = DateTime.UtcNow,
            Status = "Completed"
        });

        var result = await repository.GetByInstanceIdAsync(instance.Id);

        Assert.Single(result);
    }
}
