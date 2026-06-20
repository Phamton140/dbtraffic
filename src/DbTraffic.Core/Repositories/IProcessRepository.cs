using DbTraffic.Core.Entities;

namespace DbTraffic.Core.Repositories;

public interface IProcessRepository
{
    Task<IReadOnlyList<Process>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<Process?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Process>> GetByInstanceIdAsync(Guid instanceId, CancellationToken cancellationToken = default);
    Task<Process> CreateAsync(Process process, CancellationToken cancellationToken = default);
    Task UpdateAsync(Process process, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
