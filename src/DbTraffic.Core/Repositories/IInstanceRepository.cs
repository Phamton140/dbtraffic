using DbTraffic.Core.Entities;

namespace DbTraffic.Core.Repositories;

public interface IInstanceRepository
{
    Task<IReadOnlyList<Instance>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<Instance?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Instance> CreateAsync(Instance instance, CancellationToken cancellationToken = default);
    Task UpdateAsync(Instance instance, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
