using DbTraffic.Core.Entities;
using DbTraffic.Core.Repositories;
using DbTraffic.Infrastructure.SqlServer;
using Microsoft.Extensions.Logging;

namespace DbTraffic.Infrastructure.Discovery;

public sealed class DiscoveryService
{
    private readonly IInstanceRepository _instanceRepository;
    private readonly IDiscoveryRepository _discoveryRepository;
    private readonly ILogger<DiscoveryService> _logger;

    public DiscoveryService(
        IInstanceRepository instanceRepository,
        IDiscoveryRepository discoveryRepository,
        ILogger<DiscoveryService> logger)
    {
        _instanceRepository = instanceRepository;
        _discoveryRepository = discoveryRepository;
        _logger = logger;
    }

    public async Task DiscoverInstanceAsync(Guid instanceId, CancellationToken cancellationToken = default)
    {
        var instance = await _instanceRepository.GetByIdAsync(instanceId, cancellationToken);
        if (instance is null)
        {
            _logger.LogWarning("Instance {InstanceId} not found for discovery.", instanceId);
            return;
        }

        if (!instance.IsActive)
        {
            _logger.LogInformation("Instance {InstanceName} is inactive, skipping discovery.", instance.Name);
            return;
        }

        await DiscoverInstanceAsync(instance, cancellationToken);
    }

    public async Task DiscoverInstanceAsync(Instance instance, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting discovery for instance {InstanceName}.", instance.Name);

        try
        {
            await using var reader = new SqlServerDiscoveryReader(instance.ConnectionString);

            var jobs = await reader.GetJobsAsync(cancellationToken);
            await _discoveryRepository.SaveJobsAsync(instance.Id, jobs, cancellationToken);
            _logger.LogInformation("Discovered {JobCount} jobs for instance {InstanceName}.", jobs.Count, instance.Name);

            var objects = await reader.GetObjectsAsync(cancellationToken);
            await _discoveryRepository.SaveObjectsAsync(instance.Id, objects, cancellationToken);
            _logger.LogInformation("Discovered {ObjectCount} objects for instance {InstanceName}.", objects.Count, instance.Name);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Discovery failed for instance {InstanceName}.", instance.Name);
            throw;
        }
    }

    public async Task DiscoverAllActiveInstancesAsync(CancellationToken cancellationToken = default)
    {
        var instances = await _instanceRepository.GetAllAsync(cancellationToken);
        foreach (var instance in instances)
        {
            await DiscoverInstanceAsync(instance, cancellationToken);
        }
    }
}
