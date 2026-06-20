using DbTraffic.Core.Entities;

namespace DbTraffic.Core.Repositories;

public interface IInstanceSnapshotRepository
{
    Task<IReadOnlyList<InstanceSnapshot>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<InstanceSnapshot>> GetByInstanceIdAsync(Guid instanceId, int limit, CancellationToken cancellationToken = default);
    Task<InstanceSnapshot?> GetLatestByInstanceIdAsync(Guid instanceId, CancellationToken cancellationToken = default);
    Task<InstanceSnapshot> CreateAsync(InstanceSnapshot snapshot, CancellationToken cancellationToken = default);
    Task DeleteOldAsync(TimeSpan retention, CancellationToken cancellationToken = default);
}
