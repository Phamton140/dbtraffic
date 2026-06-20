using DbTraffic.Core.Entities;
using DbTraffic.Core.Enums;
using DbTraffic.Infrastructure.Data;
using DbTraffic.Infrastructure.Repositories;

namespace DbTraffic.Infrastructure.Tests;

public class DiscoveryRepositoryTests
{
    private const string ConnectionString = "Server=.;Database=DbTraffic;Trusted_Connection=True;TrustServerCertificate=True;";

    private static IDbConnectionFactory CreateFactory() => new SqlConnectionFactory(ConnectionString);

    private static async Task<Guid> CreateTestInstanceAsync(InstanceRepository repository)
    {
        var instance = new Instance
        {
            Name = $"Test Instance {Guid.NewGuid():N}",
            ConnectionString = ConnectionString
        };
        var created = await repository.CreateAsync(instance);
        return created.Id;
    }

    [Fact]
    public async Task SaveAndGetJobs_Should_PersistAndReturnJobs()
    {
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
}
