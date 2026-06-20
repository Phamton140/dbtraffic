using DbTraffic.Core.Entities;

namespace DbTraffic.Core.Repositories;

public interface IDiscoveryRepository
{
    Task<IReadOnlyList<DiscoveredJob>> GetJobsByInstanceAsync(Guid instanceId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DiscoveredObject>> GetObjectsByInstanceAsync(Guid instanceId, CancellationToken cancellationToken = default);
    Task SaveJobsAsync(Guid instanceId, IEnumerable<DiscoveredJob> jobs, CancellationToken cancellationToken = default);
    Task SaveObjectsAsync(Guid instanceId, IEnumerable<DiscoveredObject> objects, CancellationToken cancellationToken = default);
    Task AssociateJobAsync(Guid discoveredJobId, Guid? processId, CancellationToken cancellationToken = default);
}
