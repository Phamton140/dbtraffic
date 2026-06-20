using Dapper;
using DbTraffic.Core.Entities;
using DbTraffic.Core.Repositories;
using DbTraffic.Infrastructure.Data;

namespace DbTraffic.Infrastructure.Repositories;

public sealed class InstanceSnapshotRepository : IInstanceSnapshotRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public InstanceSnapshotRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<IReadOnlyList<InstanceSnapshot>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT Id, InstanceId, CapturedAt, CpuPercent, MemoryPercent,
                   ActiveRequests, BlockingSessions, WaitTimeMs, SnapshotJson
            FROM metrics.InstanceSnapshots
            ORDER BY CapturedAt DESC;";

        using var connection = _connectionFactory.CreateConnection();
        var snapshots = await connection.QueryAsync<InstanceSnapshot>(sql);
        return snapshots.ToList();
    }

    public async Task<IReadOnlyList<InstanceSnapshot>> GetByInstanceIdAsync(Guid instanceId, int limit, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT Id, InstanceId, CapturedAt, CpuPercent, MemoryPercent,
                   ActiveRequests, BlockingSessions, WaitTimeMs, SnapshotJson
            FROM metrics.InstanceSnapshots
            WHERE InstanceId = @InstanceId
            ORDER BY CapturedAt DESC
            OFFSET 0 ROWS FETCH NEXT @Limit ROWS ONLY;";

        using var connection = _connectionFactory.CreateConnection();
        var snapshots = await connection.QueryAsync<InstanceSnapshot>(sql, new { InstanceId = instanceId, Limit = limit });
        return snapshots.ToList();
    }

    public async Task<InstanceSnapshot?> GetLatestByInstanceIdAsync(Guid instanceId, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT TOP 1 Id, InstanceId, CapturedAt, CpuPercent, MemoryPercent,
                   ActiveRequests, BlockingSessions, WaitTimeMs, SnapshotJson
            FROM metrics.InstanceSnapshots
            WHERE InstanceId = @InstanceId
            ORDER BY CapturedAt DESC;";

        using var connection = _connectionFactory.CreateConnection();
        return await connection.QuerySingleOrDefaultAsync<InstanceSnapshot>(sql, new { InstanceId = instanceId });
    }

    public async Task<InstanceSnapshot> CreateAsync(InstanceSnapshot snapshot, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            INSERT INTO metrics.InstanceSnapshots (
                Id, InstanceId, CapturedAt, CpuPercent, MemoryPercent,
                ActiveRequests, BlockingSessions, WaitTimeMs, SnapshotJson)
            VALUES (
                @Id, @InstanceId, @CapturedAt, @CpuPercent, @MemoryPercent,
                @ActiveRequests, @BlockingSessions, @WaitTimeMs, @SnapshotJson);";

        using var connection = _connectionFactory.CreateConnection();
        await connection.ExecuteAsync(sql, snapshot);
        return snapshot;
    }

    public async Task DeleteOldAsync(TimeSpan retention, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            DELETE FROM metrics.InstanceSnapshots
            WHERE CapturedAt < @Cutoff;";

        using var connection = _connectionFactory.CreateConnection();
        await connection.ExecuteAsync(sql, new { Cutoff = DateTime.UtcNow.Subtract(retention) });
    }
}
