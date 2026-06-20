using DbTraffic.Core.Entities;

namespace DbTraffic.Core.Services;

public interface IExecutionService
{
    Task<IReadOnlyList<Execution>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Execution>> GetByInstanceIdAsync(Guid instanceId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Execution>> GetByProcessIdAsync(Guid processId, CancellationToken cancellationToken = default);
    Task<Execution?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Execution> CreateAsync(Execution execution, CancellationToken cancellationToken = default);
    Task<Execution> CompleteAsync(Guid id, DateTime completedAt, string status, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task<int> ImportFromInstanceAsync(Guid instanceId, DateTime since, CancellationToken cancellationToken = default);
    Task CalibrateProcessDurationAsync(Guid processId, CancellationToken cancellationToken = default);
}
