using DbTraffic.Shared.Models;
using DbTraffic.Shared.Models.Dmv;
using Microsoft.Data.SqlClient;

namespace DbTraffic.Infrastructure.SqlServer;

public sealed class SqlServerInstanceClient : ISqlServerInstanceClient, IAsyncDisposable
{
    private readonly string _connectionString;
    private SqlConnection? _connection;

    public SqlServerInstanceClient(InstanceConnectionInfo connectionInfo)
    {
        ArgumentNullException.ThrowIfNull(connectionInfo);
        _connectionString = connectionInfo.ConnectionString
            ?? throw new ArgumentException("ConnectionString is required", nameof(connectionInfo));
    }

    public SqlServerInstanceClient(string connectionString)
    {
        _connectionString = connectionString
            ?? throw new ArgumentNullException(nameof(connectionString));
    }

    public async Task<bool> CanConnectAsync(CancellationToken cancellationToken = default)
    {
        await EnsureConnectionAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            using var command = new SqlCommand("SELECT 1", _connection);
            await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<IReadOnlyList<ActiveRequest>> GetActiveRequestsAsync(CancellationToken cancellationToken = default)
    {
        await EnsureConnectionAsync(cancellationToken).ConfigureAwait(false);

        const string query = @"
            SELECT
                r.session_id AS SessionId,
                r.request_id AS RequestId,
                r.status AS Status,
                r.command AS Command,
                t.text AS SqlText,
                r.start_time AS StartTime,
                DB_NAME(r.database_id) AS DatabaseName,
                s.login_name AS LoginName,
                s.program_name AS ProgramName
            FROM sys.dm_exec_requests r
            INNER JOIN sys.dm_exec_sessions s ON r.session_id = s.session_id
            OUTER APPLY sys.dm_exec_sql_text(r.sql_handle) t
            WHERE r.session_id <> @@SPID
            ORDER BY r.start_time;";

        var requests = new List<ActiveRequest>();

        using var command = new SqlCommand(query, _connection);
        using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);

        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            requests.Add(new ActiveRequest
            {
                SessionId = (int)reader.GetInt16(0),
                RequestId = reader.IsDBNull(1) ? null : reader.GetInt32(1),
                Status = reader.IsDBNull(2) ? null : reader.GetString(2),
                Command = reader.IsDBNull(3) ? null : reader.GetString(3),
                SqlText = reader.IsDBNull(4) ? null : reader.GetString(4),
                StartTime = reader.IsDBNull(5) ? null : reader.GetDateTime(5),
                DatabaseName = reader.IsDBNull(6) ? null : reader.GetString(6),
                LoginName = reader.IsDBNull(7) ? null : reader.GetString(7),
                ProgramName = reader.IsDBNull(8) ? null : reader.GetString(8)
            });
        }

        return requests;
    }

    public async Task<InstanceMetrics> GetInstanceMetricsAsync(CancellationToken cancellationToken = default)
    {
        await EnsureConnectionAsync(cancellationToken).ConfigureAwait(false);

        const string query = @"
            SELECT
                (SELECT COUNT(*) FROM sys.dm_exec_requests WHERE session_id <> @@SPID) AS ActiveRequests,
                (SELECT COUNT(DISTINCT blocking_session_id) FROM sys.dm_exec_requests WHERE blocking_session_id <> 0) AS BlockingSessions,
                ISNULL((SELECT SUM(wait_time) FROM sys.dm_exec_requests WHERE session_id <> @@SPID), 0) AS WaitTimeMs,
                ISNULL((SELECT AVG(100 - SystemIdle)
                        FROM (SELECT TOP 10
                                CAST(record.value('(./Record/SchedulerMonitorEvent/SystemHealth/SystemIdle)[1]', 'INT') AS FLOAT) AS SystemIdle
                              FROM (SELECT CAST(record AS XML) AS record
                                    FROM sys.dm_os_ring_buffers
                                    WHERE ring_buffer_type = N'RING_BUFFER_SCHEDULER_MONITOR'
                                      AND record LIKE '%<SystemIdle>%') t
                              ORDER BY timestamp DESC) idle), 0) AS CpuPercent,
                ISNULL((SELECT (pm.physical_memory_in_use_kb / 1024.0) / (sm.total_physical_memory_kb / 1024.0) * 100.0
                        FROM sys.dm_os_process_memory pm
                        CROSS JOIN sys.dm_os_sys_memory sm), 0) AS MemoryPercent;";

        using var command = new SqlCommand(query, _connection);
        using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);

        var metrics = new InstanceMetrics();
        if (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            metrics.ActiveRequests = reader.IsDBNull(0) ? 0 : reader.GetInt32(0);
            metrics.BlockingSessions = reader.IsDBNull(1) ? 0 : reader.GetInt32(1);
            metrics.WaitTimeMs = reader.IsDBNull(2) ? 0 : reader.GetInt64(2);
            metrics.CpuPercent = reader.IsDBNull(3) ? 0 : reader.GetDouble(3);
            metrics.MemoryPercent = reader.IsDBNull(4) ? 0 : reader.GetDouble(4);
        }

        metrics.ActiveRequestsDetail = await GetActiveRequestsAsync(cancellationToken).ConfigureAwait(false);
        return metrics;
    }

    public async Task<IReadOnlyList<JobHistoryEntry>> GetJobHistoryAsync(DateTime since, CancellationToken cancellationToken = default)
    {
        await EnsureConnectionAsync(cancellationToken).ConfigureAwait(false);

        const string query = @"
            SELECT
                j.job_id AS JobId,
                j.name AS JobName,
                h.step_id AS StepId,
                h.step_name AS StepName,
                msdb.dbo.agent_datetime(h.run_date, h.run_time) AS RunDateTime,
                ((h.run_duration / 10000) * 60) + ((h.run_duration / 100) % 100) + (h.run_duration % 100 / 60.0) AS DurationMinutes,
                CASE h.run_status
                    WHEN 0 THEN 'Failed'
                    WHEN 1 THEN 'Succeeded'
                    WHEN 2 THEN 'Retry'
                    WHEN 3 THEN 'Canceled'
                    ELSE 'Unknown'
                END AS Status,
                h.message AS Message
            FROM msdb.dbo.sysjobhistory h
            INNER JOIN msdb.dbo.sysjobs j ON h.job_id = j.job_id
            WHERE msdb.dbo.agent_datetime(h.run_date, h.run_time) >= @Since
            ORDER BY RunDateTime DESC;";

        var entries = new List<JobHistoryEntry>();

        using var command = new SqlCommand(query, _connection);
        command.Parameters.AddWithValue("@Since", since);
        using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);

        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            entries.Add(new JobHistoryEntry
            {
                JobId = reader.GetGuid(0),
                JobName = reader.IsDBNull(1) ? string.Empty : reader.GetString(1),
                StepId = reader.GetInt32(2),
                StepName = reader.IsDBNull(3) ? string.Empty : reader.GetString(3),
                RunDateTime = reader.GetDateTime(4),
                DurationMinutes = reader.IsDBNull(5) ? 0 : (int)Math.Round(reader.GetDouble(5)),
                Status = reader.IsDBNull(6) ? string.Empty : reader.GetString(6),
                Message = reader.IsDBNull(7) ? null : reader.GetString(7)
            });
        }

        return entries;
    }

    private async Task EnsureConnectionAsync(CancellationToken cancellationToken)
    {
        if (_connection is null)
        {
            _connection = new SqlConnection(_connectionString);
        }

        if (_connection.State != System.Data.ConnectionState.Open)
        {
            await _connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_connection is not null)
        {
            await _connection.DisposeAsync().ConfigureAwait(false);
            _connection = null;
        }
    }
}
