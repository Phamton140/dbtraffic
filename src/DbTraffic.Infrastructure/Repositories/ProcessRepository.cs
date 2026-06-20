using System.Data;
using Dapper;
using DbTraffic.Core.Entities;
using DbTraffic.Core.Repositories;
using DbTraffic.Infrastructure.Data;

namespace DbTraffic.Infrastructure.Repositories;

public sealed class ProcessRepository : IProcessRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public ProcessRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<IReadOnlyList<Process>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT Id, InstanceId, Name, ProcessType, Description, EstimatedDurationMinutes,
                   PreferredWindowStart, PreferredWindowEnd, CpuIntensity, IoIntensity, MemoryIntensity,
                   IsActive, CreatedAt, UpdatedAt
            FROM catalog.Processes
            WHERE IsActive = 1
            ORDER BY Name;";

        using var connection = _connectionFactory.CreateConnection();
        var processes = await connection.QueryAsync<Process>(sql);
        return processes.ToList();
    }

    public async Task<Process?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT Id, InstanceId, Name, ProcessType, Description, EstimatedDurationMinutes,
                   PreferredWindowStart, PreferredWindowEnd, CpuIntensity, IoIntensity, MemoryIntensity,
                   IsActive, CreatedAt, UpdatedAt
            FROM catalog.Processes
            WHERE Id = @Id;

            SELECT Id, ProcessId, SchemaName, ObjectName, ObjectType, IsCritical, AccessType, CreatedAt
            FROM catalog.ProcessObjects
            WHERE ProcessId = @Id;

            SELECT Id, ProcessId, DayOfWeek, StartTime, DurationMinutes, IsActive, CreatedAt
            FROM catalog.ProcessSchedules
            WHERE ProcessId = @Id AND IsActive = 1;";

        using var connection = _connectionFactory.CreateConnection();
        using var multi = await connection.QueryMultipleAsync(sql, new { Id = id });

        var process = await multi.ReadSingleOrDefaultAsync<Process>();
        if (process is null)
        {
            return null;
        }

        var objects = await multi.ReadAsync<ProcessObject>();
        var schedules = await multi.ReadAsync<ProcessSchedule>();

        process.Objects.AddRange(objects);
        process.Schedules.AddRange(schedules);

        return process;
    }

    public async Task<IReadOnlyList<Process>> GetByInstanceIdAsync(Guid instanceId, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT Id, InstanceId, Name, ProcessType, Description, EstimatedDurationMinutes,
                   PreferredWindowStart, PreferredWindowEnd, CpuIntensity, IoIntensity, MemoryIntensity,
                   IsActive, CreatedAt, UpdatedAt
            FROM catalog.Processes
            WHERE InstanceId = @InstanceId AND IsActive = 1
            ORDER BY Name;";

        using var connection = _connectionFactory.CreateConnection();
        var processes = await connection.QueryAsync<Process>(sql, new { InstanceId = instanceId });
        return processes.ToList();
    }

    public async Task<Process> CreateAsync(Process process, CancellationToken cancellationToken = default)
    {
        process.Validate();
        process.Touch();

        const string processSql = @"
            INSERT INTO catalog.Processes (
                Id, InstanceId, Name, ProcessType, Description, EstimatedDurationMinutes,
                PreferredWindowStart, PreferredWindowEnd, CpuIntensity, IoIntensity, MemoryIntensity,
                IsActive, CreatedAt, UpdatedAt)
            VALUES (
                @Id, @InstanceId, @Name, @ProcessType, @Description, @EstimatedDurationMinutes,
                @PreferredWindowStart, @PreferredWindowEnd, @CpuIntensity, @IoIntensity, @MemoryIntensity,
                @IsActive, @CreatedAt, @UpdatedAt);";

        const string objectSql = @"
            INSERT INTO catalog.ProcessObjects (Id, ProcessId, SchemaName, ObjectName, ObjectType, IsCritical, AccessType, CreatedAt)
            VALUES (@Id, @ProcessId, @SchemaName, @ObjectName, @ObjectType, @IsCritical, @AccessType, @CreatedAt);";

        const string scheduleSql = @"
            INSERT INTO catalog.ProcessSchedules (Id, ProcessId, DayOfWeek, StartTime, DurationMinutes, IsActive, CreatedAt)
            VALUES (@Id, @ProcessId, @DayOfWeek, @StartTime, @DurationMinutes, @IsActive, @CreatedAt);";

        using var connection = _connectionFactory.CreateConnection();
        connection.Open();
        using var transaction = connection.BeginTransaction();

        try
        {
            await connection.ExecuteAsync(processSql, process, transaction);

            foreach (var obj in process.Objects)
            {
                obj.ProcessId = process.Id;
                await connection.ExecuteAsync(objectSql, obj, transaction);
            }

            foreach (var schedule in process.Schedules)
            {
                schedule.ProcessId = process.Id;
                await connection.ExecuteAsync(scheduleSql, schedule, transaction);
            }

            transaction.Commit();
            return process;
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    public async Task UpdateAsync(Process process, CancellationToken cancellationToken = default)
    {
        process.Validate();
        process.Touch();

        const string sql = @"
            UPDATE catalog.Processes
            SET InstanceId = @InstanceId,
                Name = @Name,
                ProcessType = @ProcessType,
                Description = @Description,
                EstimatedDurationMinutes = @EstimatedDurationMinutes,
                PreferredWindowStart = @PreferredWindowStart,
                PreferredWindowEnd = @PreferredWindowEnd,
                CpuIntensity = @CpuIntensity,
                IoIntensity = @IoIntensity,
                MemoryIntensity = @MemoryIntensity,
                IsActive = @IsActive,
                UpdatedAt = @UpdatedAt
            WHERE Id = @Id;";

        using var connection = _connectionFactory.CreateConnection();
        await connection.ExecuteAsync(sql, process);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        const string sql = "UPDATE catalog.Processes SET IsActive = 0, UpdatedAt = GETUTCDATE() WHERE Id = @Id;";

        using var connection = _connectionFactory.CreateConnection();
        await connection.ExecuteAsync(sql, new { Id = id });
    }
}
