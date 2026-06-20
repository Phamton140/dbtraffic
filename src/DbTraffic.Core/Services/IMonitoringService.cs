using DbTraffic.Core.Entities;
using DbTraffic.Shared.Models.Dmv;

namespace DbTraffic.Core.Services;

public interface IMonitoringService
{
    Task<InstanceSnapshot> CaptureSnapshotAsync(Guid instanceId, CancellationToken cancellationToken = default);
    Task<InstanceMetrics> GetCurrentMetricsAsync(Guid instanceId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ActiveRequest>> GetActiveRequestsAsync(Guid instanceId, CancellationToken cancellationToken = default);
}
