using Dapper;
using DbTraffic.Core.Entities;
using DbTraffic.Core.Repositories;
using DbTraffic.Infrastructure.Data;

namespace DbTraffic.Infrastructure.Repositories;

public sealed class ExecutionRepository : IExecutionRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public ExecutionRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<IReadOnlyList<Execution>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT Id, ProcessId, InstanceId, Source, StartedAt, CompletedAt,
                   DurationMinutes, Status, AffectedObjectsJson, Notes, CreatedAt
            FROM history.Executions
            ORDER BY StartedAt DESC;";

        using var connection = _connectionFactory.CreateConnection();
        var executions = await connection.QueryAsync<Execution>(sql);
        return executions.ToList();
    }

    public async Task<IReadOnlyList<Execution>> GetByInstanceIdAsync(Guid instanceId, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT Id, ProcessId, InstanceId, Source, StartedAt, CompletedAt,
                   DurationMinutes, Status, AffectedObjectsJson, Notes, CreatedAt
            FROM history.Executions
            WHERE InstanceId = @InstanceId
            ORDER BY StartedAt DESC;";

        using var connection = _connectionFactory.CreateConnection();
        var executions = await connection.QueryAsync<Execution>(sql, new { InstanceId = instanceId });
        return executions.ToList();
    }

    public async Task<IReadOnlyList<Execution>> GetByProcessIdAsync(Guid processId, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT Id, ProcessId, InstanceId, Source, StartedAt, CompletedAt,
                   DurationMinutes, Status, AffectedObjectsJson, Notes, CreatedAt
            FROM history.Executions
            WHERE ProcessId = @ProcessId
            ORDER BY StartedAt DESC;";

        using var connection = _connectionFactory.CreateConnection();
        var executions = await connection.QueryAsync<Execution>(sql, new { ProcessId = processId });
        return executions.ToList();
    }

    public async Task<Execution?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT Id, ProcessId, InstanceId, Source, StartedAt, CompletedAt,
                   DurationMinutes, Status, AffectedObjectsJson, Notes, CreatedAt
            FROM history.Executions
            WHERE Id = @Id;";

        using var connection = _connectionFactory.CreateConnection();
        return await connection.QuerySingleOrDefaultAsync<Execution>(sql, new { Id = id });
    }

    public async Task<Execution> CreateAsync(Execution execution, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            INSERT INTO history.Executions (
                Id, ProcessId, InstanceId, Source, StartedAt, CompletedAt,
                DurationMinutes, Status, AffectedObjectsJson, Notes, CreatedAt)
            VALUES (
                @Id, @ProcessId, @InstanceId, @Source, @StartedAt, @CompletedAt,
                @DurationMinutes, @Status, @AffectedObjectsJson, @Notes, @CreatedAt);";

        using var connection = _connectionFactory.CreateConnection();
        await connection.ExecuteAsync(sql, execution);
        return execution;
    }

    public async Task UpdateAsync(Execution execution, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            UPDATE history.Executions
            SET ProcessId = @ProcessId,
                InstanceId = @InstanceId,
                Source = @Source,
                StartedAt = @StartedAt,
                CompletedAt = @CompletedAt,
                DurationMinutes = @DurationMinutes,
                Status = @Status,
                AffectedObjectsJson = @AffectedObjectsJson,
                Notes = @Notes
            WHERE Id = @Id;";

        using var connection = _connectionFactory.CreateConnection();
        await connection.ExecuteAsync(sql, execution);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        const string sql = "DELETE FROM history.Executions WHERE Id = @Id;";

        using var connection = _connectionFactory.CreateConnection();
        await connection.ExecuteAsync(sql, new { Id = id });
    }
}
