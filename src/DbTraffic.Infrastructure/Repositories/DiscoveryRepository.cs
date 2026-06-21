using System.Data;
using Dapper;
using DbTraffic.Core.Entities;
using DbTraffic.Core.Repositories;
using DbTraffic.Infrastructure.Data;

namespace DbTraffic.Infrastructure.Repositories;

public sealed class DiscoveryRepository : IDiscoveryRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public DiscoveryRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<IReadOnlyList<DiscoveredJob>> GetJobsByInstanceAsync(Guid instanceId, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT Id, InstanceId, JobId, Name, Description, Enabled, EstimatedDurationMinutes, LastRunDate, NextRunDate, DiscoveredAt, AssociatedProcessId
            FROM catalog.DiscoveredJobs
            WHERE InstanceId = @InstanceId
            ORDER BY Name;";

        const string schedulesSql = @"
            SELECT Id, DiscoveredJobId, ScheduleId, Name, FrequencyType, FrequencyInterval, FrequencySubdayType, FrequencySubdayInterval, FrequencyRelativeInterval, FrequencyRecurrenceFactor, ActiveStartTime, ActiveEndTime, Description, DiscoveredAt
            FROM catalog.DiscoveredJobSchedules
            WHERE DiscoveredJobId IN (SELECT Id FROM catalog.DiscoveredJobs WHERE InstanceId = @InstanceId)
            ORDER BY DiscoveredJobId, ScheduleId;";

        using var connection = _connectionFactory.CreateConnection();
        var jobs = (await connection.QueryAsync<DiscoveredJob>(sql, new { InstanceId = instanceId })).ToList();
        var schedules = (await connection.QueryAsync<DiscoveredJobSchedule>(schedulesSql, new { InstanceId = instanceId })).ToList();

        foreach (var job in jobs)
        {
            job.Schedules = schedules.Where(s => s.DiscoveredJobId == job.Id).ToList();
        }

        return jobs;
    }

    public async Task<IReadOnlyList<DiscoveredObject>> GetObjectsByInstanceAsync(Guid instanceId, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT Id, InstanceId, SchemaName, ObjectName, ObjectType, DiscoveredAt
            FROM catalog.DiscoveredObjects
            WHERE InstanceId = @InstanceId
            ORDER BY SchemaName, ObjectName;";

        using var connection = _connectionFactory.CreateConnection();
        var objects = await connection.QueryAsync<DiscoveredObject>(sql, new { InstanceId = instanceId });
        return objects.ToList();
    }

    public async Task SaveJobsAsync(Guid instanceId, IEnumerable<DiscoveredJob> jobs, CancellationToken cancellationToken = default)
    {
        var discoveredJobs = jobs.ToList();
        foreach (var job in discoveredJobs)
        {
            job.InstanceId = instanceId;
        }

        const string selectSql = "SELECT Id, JobId, AssociatedProcessId FROM catalog.DiscoveredJobs WHERE InstanceId = @InstanceId;";
        const string updateSql = @"
            UPDATE catalog.DiscoveredJobs
            SET Name = @Name,
                Description = @Description,
                Enabled = @Enabled,
                EstimatedDurationMinutes = @EstimatedDurationMinutes,
                LastRunDate = @LastRunDate,
                NextRunDate = @NextRunDate,
                DiscoveredAt = @DiscoveredAt
            WHERE Id = @Id;";
        const string insertSql = @"
            INSERT INTO catalog.DiscoveredJobs (Id, InstanceId, JobId, Name, Description, Enabled, EstimatedDurationMinutes, LastRunDate, NextRunDate, DiscoveredAt, AssociatedProcessId)
            VALUES (@Id, @InstanceId, @JobId, @Name, @Description, @Enabled, @EstimatedDurationMinutes, @LastRunDate, @NextRunDate, @DiscoveredAt, @AssociatedProcessId);";
        const string deleteSchedulesSql = "DELETE FROM catalog.DiscoveredJobSchedules WHERE DiscoveredJobId = @DiscoveredJobId;";
        const string insertScheduleSql = @"
            INSERT INTO catalog.DiscoveredJobSchedules (Id, DiscoveredJobId, ScheduleId, Name, FrequencyType, FrequencyInterval, FrequencySubdayType, FrequencySubdayInterval, FrequencyRelativeInterval, FrequencyRecurrenceFactor, ActiveStartTime, ActiveEndTime, Description, DiscoveredAt)
            VALUES (@Id, @DiscoveredJobId, @ScheduleId, @Name, @FrequencyType, @FrequencyInterval, @FrequencySubdayType, @FrequencySubdayInterval, @FrequencyRelativeInterval, @FrequencyRecurrenceFactor, @ActiveStartTime, @ActiveEndTime, @Description, @DiscoveredAt);";

        using var connection = _connectionFactory.CreateConnection();
        connection.Open();
        using var transaction = connection.BeginTransaction();

        try
        {
            var existing = await connection.QueryAsync<(Guid Id, Guid JobId, Guid? AssociatedProcessId)>(
                selectSql,
                new { InstanceId = instanceId },
                transaction);

            var existingByJobId = existing.ToDictionary(x => x.JobId, x => x);

            foreach (var job in discoveredJobs)
            {
                if (existingByJobId.TryGetValue(job.JobId, out var existingJob))
                {
                    job.Id = existingJob.Id;
                    job.AssociatedProcessId = existingJob.AssociatedProcessId;
                    await connection.ExecuteAsync(updateSql, job, transaction);
                }
                else
                {
                    await connection.ExecuteAsync(insertSql, job, transaction);
                }

                await connection.ExecuteAsync(deleteSchedulesSql, new { DiscoveredJobId = job.Id }, transaction);

                foreach (var schedule in job.Schedules)
                {
                    schedule.DiscoveredJobId = job.Id;
                }

                if (job.Schedules.Count > 0)
                {
                    await connection.ExecuteAsync(insertScheduleSql, job.Schedules, transaction);
                }
            }

            transaction.Commit();
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    public async Task SaveObjectsAsync(Guid instanceId, IEnumerable<DiscoveredObject> objects, CancellationToken cancellationToken = default)
    {
        var discoveredObjects = objects.ToList();
        foreach (var obj in discoveredObjects)
        {
            obj.InstanceId = instanceId;
        }

        const string deleteSql = "DELETE FROM catalog.DiscoveredObjects WHERE InstanceId = @InstanceId;";
        const string insertSql = @"
            INSERT INTO catalog.DiscoveredObjects (Id, InstanceId, SchemaName, ObjectName, ObjectType, DiscoveredAt)
            VALUES (@Id, @InstanceId, @SchemaName, @ObjectName, @ObjectType, @DiscoveredAt);";

        using var connection = _connectionFactory.CreateConnection();
        connection.Open();
        using var transaction = connection.BeginTransaction();

        try
        {
            await connection.ExecuteAsync(deleteSql, new { InstanceId = instanceId }, transaction);
            await connection.ExecuteAsync(insertSql, discoveredObjects, transaction);
            transaction.Commit();
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    public async Task AssociateJobAsync(Guid discoveredJobId, Guid? processId, CancellationToken cancellationToken = default)
    {
        const string sql = "UPDATE catalog.DiscoveredJobs SET AssociatedProcessId = @ProcessId WHERE Id = @Id;";

        using var connection = _connectionFactory.CreateConnection();
        await connection.ExecuteAsync(sql, new { Id = discoveredJobId, ProcessId = processId });
    }
}
