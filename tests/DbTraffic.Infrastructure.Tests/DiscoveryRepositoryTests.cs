using DbTraffic.Core.Entities;
using DbTraffic.Core.Enums;
using DbTraffic.Infrastructure.Data;
using DbTraffic.Infrastructure.Repositories;

namespace DbTraffic.Infrastructure.Tests;

[Collection("SqlServer collection")]
public class DiscoveryRepositoryTests
{
    private readonly SqlServerTestFixture _fixture;

    public DiscoveryRepositoryTests(SqlServerTestFixture fixture)
    {
        _fixture = fixture;
    }

    private IDbConnectionFactory CreateFactory() => _fixture.CreateConnectionFactory();

    private async Task<Guid> CreateTestInstanceAsync(InstanceRepository repository)
    {
        var instance = new Instance
        {
            Name = $"Test Instance {Guid.NewGuid():N}",
            ConnectionString = _fixture.ConnectionString
        };
        var created = await repository.CreateAsync(instance);
        return created.Id;
    }

    [Fact]
    public async Task SaveAndGetJobs_Should_PersistAndReturnJobs()
    {
        if (!_fixture.IsAvailable)
        {
            return;
        }

        var instanceRepository = new InstanceRepository(CreateFactory());
        var repository = new DiscoveryRepository(CreateFactory());
        var instanceId = await CreateTestInstanceAsync(instanceRepository);

        var jobs = new List<DiscoveredJob>
        {
            new()
            {
                JobId = Guid.NewGuid(),
                Name = "Job A",
                Description = "Test job A",
                Enabled = true
            },
            new()
            {
                JobId = Guid.NewGuid(),
                Name = "Job B",
                Description = "Test job B",
                Enabled = false
            }
        };

        await repository.SaveJobsAsync(instanceId, jobs);

        var result = await repository.GetJobsByInstanceAsync(instanceId);

        Assert.Equal(2, result.Count);
        Assert.Contains(result, j => j.Name == "Job A" && j.Enabled);
        Assert.Contains(result, j => j.Name == "Job B" && !j.Enabled);
    }

    [Fact]
    public async Task AssociateJob_Should_UpdateAssociatedProcessId()
    {
        if (!_fixture.IsAvailable)
        {
            return;
        }

        var instanceRepository = new InstanceRepository(CreateFactory());
        var processRepository = new ProcessRepository(CreateFactory());
        var repository = new DiscoveryRepository(CreateFactory());
        var instanceId = await CreateTestInstanceAsync(instanceRepository);

        var process = await processRepository.CreateAsync(new Process
        {
            InstanceId = instanceId,
            Name = "Associated Process",
            ProcessType = ProcessType.SqlAgentJob
        });
        var processId = process.Id;

        var job = new DiscoveredJob
        {
            JobId = Guid.NewGuid(),
            Name = "Job To Associate",
            Enabled = true
        };

        await repository.SaveJobsAsync(instanceId, new[] { job });

        var saved = (await repository.GetJobsByInstanceAsync(instanceId)).First();
        Assert.Null(saved.AssociatedProcessId);

        await repository.AssociateJobAsync(saved.Id, processId);

        var associated = (await repository.GetJobsByInstanceAsync(instanceId)).First();
        Assert.Equal(processId, associated.AssociatedProcessId);
    }

    [Fact]
    public async Task SaveAndGetObjects_Should_PersistAndReturnObjects()
    {
        if (!_fixture.IsAvailable)
        {
            return;
        }

        var instanceRepository = new InstanceRepository(CreateFactory());
        var repository = new DiscoveryRepository(CreateFactory());
        var instanceId = await CreateTestInstanceAsync(instanceRepository);

        var objects = new List<DiscoveredObject>
        {
            new()
            {
                SchemaName = "dbo",
                ObjectName = "Orders",
                ObjectType = "USER_TABLE"
            },
            new()
            {
                SchemaName = "dbo",
                ObjectName = "GetOrders",
                ObjectType = "SQL_STORED_PROCEDURE"
            }
        };

        await repository.SaveObjectsAsync(instanceId, objects);

        var result = await repository.GetObjectsByInstanceAsync(instanceId);

        Assert.Equal(2, result.Count);
        Assert.Contains(result, o => o.ObjectName == "Orders" && o.ObjectType == "USER_TABLE");
        Assert.Contains(result, o => o.ObjectName == "GetOrders" && o.ObjectType == "SQL_STORED_PROCEDURE");
    }

    [Fact]
    public async Task SaveJobsAsync_With_Schedules_Should_Persist_Schedules()
    {
        if (!_fixture.IsAvailable)
        {
            return;
        }

        var instanceRepository = new InstanceRepository(CreateFactory());
        var repository = new DiscoveryRepository(CreateFactory());
        var instanceId = await CreateTestInstanceAsync(instanceRepository);

        var job = new DiscoveredJob
        {
            JobId = Guid.NewGuid(),
            Name = "Job With Schedules",
            Enabled = true,
            Schedules = new List<DiscoveredJobSchedule>
            {
                new()
                {
                    ScheduleId = 1,
                    Name = "Daily Schedule",
                    FrequencyType = 4,
                    FrequencyInterval = 1,
                    ActiveStartTime = new TimeSpan(8, 0, 0),
                    ActiveEndTime = new TimeSpan(18, 0, 0)
                },
                new()
                {
                    ScheduleId = 2,
                    Name = "Weekly Schedule",
                    FrequencyType = 8,
                    FrequencyInterval = 2 + 4 + 8 + 16 + 32,
                    ActiveStartTime = new TimeSpan(9, 0, 0),
                    ActiveEndTime = new TimeSpan(17, 0, 0)
                }
            }
        };

        await repository.SaveJobsAsync(instanceId, new[] { job });

        var result = await repository.GetJobsByInstanceAsync(instanceId);

        var savedJob = Assert.Single(result);
        Assert.Equal(2, savedJob.Schedules.Count);
        Assert.Contains(savedJob.Schedules, s => s.Name == "Daily Schedule" && s.FrequencyType == 4);
        Assert.Contains(savedJob.Schedules, s => s.Name == "Weekly Schedule" && s.FrequencyType == 8);
    }

    [Fact]
    public async Task SaveJobsAsync_Empty_List_Should_Not_Throw()
    {
        if (!_fixture.IsAvailable)
        {
            return;
        }

        var repository = new DiscoveryRepository(CreateFactory());
        var instanceId = Guid.NewGuid();

        var exception = await Record.ExceptionAsync(() =>
            repository.SaveJobsAsync(instanceId, new List<DiscoveredJob>()));

        Assert.Null(exception);
    }

    [Fact]
    public async Task SaveObjectsAsync_Empty_List_Should_Not_Throw()
    {
        if (!_fixture.IsAvailable)
        {
            return;
        }

        var repository = new DiscoveryRepository(CreateFactory());
        var instanceId = Guid.NewGuid();

        var exception = await Record.ExceptionAsync(() =>
            repository.SaveObjectsAsync(instanceId, new List<DiscoveredObject>()));

        Assert.Null(exception);
    }

    [Fact]
    public async Task GetJobsByInstanceAsync_When_No_Jobs_Returns_Empty()
    {
        if (!_fixture.IsAvailable)
        {
            return;
        }

        var instanceRepository = new InstanceRepository(CreateFactory());
        var repository = new DiscoveryRepository(CreateFactory());
        var instanceId = await CreateTestInstanceAsync(instanceRepository);

        var result = await repository.GetJobsByInstanceAsync(instanceId);

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetObjectsByInstanceAsync_When_No_Objects_Returns_Empty()
    {
        if (!_fixture.IsAvailable)
        {
            return;
        }

        var instanceRepository = new InstanceRepository(CreateFactory());
        var repository = new DiscoveryRepository(CreateFactory());
        var instanceId = await CreateTestInstanceAsync(instanceRepository);

        var result = await repository.GetObjectsByInstanceAsync(instanceId);

        Assert.Empty(result);
    }

    [Fact]
    public async Task AssociateJobAsync_With_Null_ProcessId_Should_Clear_Association()
    {
        if (!_fixture.IsAvailable)
        {
            return;
        }

        var instanceRepository = new InstanceRepository(CreateFactory());
        var repository = new DiscoveryRepository(CreateFactory());
        var instanceId = await CreateTestInstanceAsync(instanceRepository);

        var job = new DiscoveredJob
        {
            JobId = Guid.NewGuid(),
            Name = "Job To Disassociate",
            Enabled = true
        };

        await repository.SaveJobsAsync(instanceId, new[] { job });

        var saved = (await repository.GetJobsByInstanceAsync(instanceId)).First();
        Assert.Null(saved.AssociatedProcessId);

        await repository.AssociateJobAsync(saved.Id, Guid.NewGuid());

        var associated = (await repository.GetJobsByInstanceAsync(instanceId)).First();
        Assert.NotNull(associated.AssociatedProcessId);

        await repository.AssociateJobAsync(saved.Id, null);

        var disassociated = (await repository.GetJobsByInstanceAsync(instanceId)).First();
        Assert.Null(disassociated.AssociatedProcessId);
    }

    [Fact]
    public async Task SaveJobsAsync_Upsert_Existing_Job_Updates_Properties()
    {
        if (!_fixture.IsAvailable)
        {
            return;
        }

        var instanceRepository = new InstanceRepository(CreateFactory());
        var repository = new DiscoveryRepository(CreateFactory());
        var instanceId = await CreateTestInstanceAsync(instanceRepository);

        var job = new DiscoveredJob
        {
            JobId = Guid.NewGuid(),
            Name = "Original Name",
            Description = "Original description",
            Enabled = true
        };

        await repository.SaveJobsAsync(instanceId, new[] { job });

        var updatedJob = new DiscoveredJob
        {
            JobId = job.JobId,
            Name = "Updated Name",
            Description = "Updated description",
            Enabled = false,
            EstimatedDurationMinutes = 30
        };

        await repository.SaveJobsAsync(instanceId, new[] { updatedJob });

        var result = await repository.GetJobsByInstanceAsync(instanceId);
        var savedJob = Assert.Single(result);
        Assert.Equal("Updated Name", savedJob.Name);
        Assert.Equal("Updated description", savedJob.Description);
        Assert.False(savedJob.Enabled);
        Assert.Equal(30, savedJob.EstimatedDurationMinutes);
    }
}
