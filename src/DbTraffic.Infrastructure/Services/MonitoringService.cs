using DbTraffic.Core.Entities;
using DbTraffic.Core.Repositories;
using DbTraffic.Core.Services;
using DbTraffic.Infrastructure.SqlServer;
using DbTraffic.Shared.Models.Dmv;
using Microsoft.Extensions.Logging;

namespace DbTraffic.Infrastructure.Services;

public sealed class MonitoringService : IMonitoringService
{
    private readonly IInstanceRepository _instanceRepository;
    private readonly IInstanceSnapshotRepository _snapshotRepository;
    private readonly ILogger<SqlServerInstanceClient> _instanceClientLogger;

    public MonitoringService(
        IInstanceRepository instanceRepository,
        IInstanceSnapshotRepository snapshotRepository,
        ILogger<SqlServerInstanceClient> instanceClientLogger)
    {
        _instanceRepository = instanceRepository;
        _snapshotRepository = snapshotRepository;
        _instanceClientLogger = instanceClientLogger;
    }

    public async Task<InstanceSnapshot> CaptureSnapshotAsync(Guid instanceId, CancellationToken cancellationToken = default)
    {
        var instance = await _instanceRepository.GetByIdAsync(instanceId, cancellationToken);
        if (instance is null)
        {
            throw new InvalidOperationException($"Instance {instanceId} not found.");
        }

        var metrics = await GetMetricsInternalAsync(instance.ConnectionString, cancellationToken);

        var snapshot = new InstanceSnapshot
        {
            InstanceId = instanceId,
            CpuPercent = (decimal?)metrics.CpuPercent,
            MemoryPercent = (decimal?)metrics.MemoryPercent,
            ActiveRequests = metrics.ActiveRequests,
            BlockingSessions = metrics.BlockingSessions,
            WaitTimeMs = metrics.WaitTimeMs,
            SnapshotJson = System.Text.Json.JsonSerializer.Serialize(metrics.ActiveRequestsDetail)
        };

        await _snapshotRepository.CreateAsync(snapshot, cancellationToken);
        return snapshot;
    }

    public async Task<InstanceMetrics> GetCurrentMetricsAsync(Guid instanceId, CancellationToken cancellationToken = default)
    {
        var instance = await _instanceRepository.GetByIdAsync(instanceId, cancellationToken);
        if (instance is null)
        {
            throw new InvalidOperationException($"Instance {instanceId} not found.");
        }

        return await GetMetricsInternalAsync(instance.ConnectionString, cancellationToken);
    }

    public async Task<IReadOnlyList<ActiveRequest>> GetActiveRequestsAsync(Guid instanceId, CancellationToken cancellationToken = default)
    {
        var instance = await _instanceRepository.GetByIdAsync(instanceId, cancellationToken);
        if (instance is null)
        {
            throw new InvalidOperationException($"Instance {instanceId} not found.");
        }

        await using var client = new SqlServerInstanceClient(instance.ConnectionString, _instanceClientLogger);
        return await client.GetActiveRequestsAsync(cancellationToken);
    }

    private async Task<InstanceMetrics> GetMetricsInternalAsync(string connectionString, CancellationToken cancellationToken)
    {
        await using var client = new SqlServerInstanceClient(connectionString, _instanceClientLogger);
        return await client.GetInstanceMetricsAsync(cancellationToken);
    }
}
