using DbTraffic.Shared.Models;
using DbTraffic.Shared.Models.Dmv;

namespace DbTraffic.Infrastructure.SqlServer;

public interface ISqlServerInstanceClient
{
    Task<bool> CanConnectAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ActiveRequest>> GetActiveRequestsAsync(CancellationToken cancellationToken = default);
    Task<InstanceMetrics> GetInstanceMetricsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<JobHistoryEntry>> GetJobHistoryAsync(DateTime since, CancellationToken cancellationToken = default);
}
