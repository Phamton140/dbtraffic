using DbTraffic.Core.Entities;

namespace DbTraffic.Core.Repositories;

public interface IExecutionRepository
{
    Task<IReadOnlyList<Execution>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Execution>> GetByInstanceIdAsync(Guid instanceId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Execution>> GetByProcessIdAsync(Guid processId, CancellationToken cancellationToken = default);
    Task<Execution?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Execution> CreateAsync(Execution execution, CancellationToken cancellationToken = default);
    Task UpdateAsync(Execution execution, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
