using Dapper;
using DbTraffic.Core.Entities;
using DbTraffic.Core.Repositories;
using DbTraffic.Infrastructure.Data;

namespace DbTraffic.Infrastructure.Repositories;

public sealed class InstanceRepository : IInstanceRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public InstanceRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<IReadOnlyList<Instance>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT Id, Name, ConnectionString, Description, IsActive, CreatedAt, UpdatedAt
            FROM catalog.Instances
            WHERE IsActive = 1
            ORDER BY Name;";

        using var connection = _connectionFactory.CreateConnection();
        var instances = await connection.QueryAsync<Instance>(sql);
        return instances.ToList();
    }

    public async Task<Instance?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT Id, Name, ConnectionString, Description, IsActive, CreatedAt, UpdatedAt
            FROM catalog.Instances
            WHERE Id = @Id;";

        using var connection = _connectionFactory.CreateConnection();
        return await connection.QuerySingleOrDefaultAsync<Instance>(sql, new { Id = id });
    }

    public async Task<Instance> CreateAsync(Instance instance, CancellationToken cancellationToken = default)
    {
        instance.Validate();
        instance.Touch();

        const string sql = @"
            INSERT INTO catalog.Instances (Id, Name, ConnectionString, Description, IsActive, CreatedAt, UpdatedAt)
            VALUES (@Id, @Name, @ConnectionString, @Description, @IsActive, @CreatedAt, @UpdatedAt);";

        using var connection = _connectionFactory.CreateConnection();
        await connection.ExecuteAsync(sql, instance);
        return instance;
    }

    public async Task UpdateAsync(Instance instance, CancellationToken cancellationToken = default)
    {
        instance.Validate();
        instance.Touch();

        const string sql = @"
            UPDATE catalog.Instances
            SET Name = @Name,
                ConnectionString = @ConnectionString,
                Description = @Description,
                IsActive = @IsActive,
                UpdatedAt = @UpdatedAt
            WHERE Id = @Id;";

        using var connection = _connectionFactory.CreateConnection();
        await connection.ExecuteAsync(sql, instance);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        const string sql = "UPDATE catalog.Instances SET IsActive = 0, UpdatedAt = GETUTCDATE() WHERE Id = @Id;";

        using var connection = _connectionFactory.CreateConnection();
        await connection.ExecuteAsync(sql, new { Id = id });
    }
}
