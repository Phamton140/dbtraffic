using DbTraffic.Core.Entities;

namespace DbTraffic.Infrastructure.SqlServer;

public interface ISqlServerDiscoveryReader
{
    Task<IReadOnlyList<DiscoveredJob>> GetJobsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DiscoveredObject>> GetObjectsAsync(CancellationToken cancellationToken = default);
}
