using DbTraffic.Shared.Models;
using DbTraffic.Shared.Models.Dmv;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;

namespace DbTraffic.Infrastructure.SqlServer;

public sealed class SqlServerInstanceClient : ISqlServerInstanceClient, IAsyncDisposable
{
    private readonly string _connectionString;
    private readonly ILogger<SqlServerInstanceClient>? _logger;
    private SqlConnection? _connection;

    public SqlServerInstanceClient(InstanceConnectionInfo connectionInfo, ILogger<SqlServerInstanceClient> logger)
    {
        ArgumentNullException.ThrowIfNull(connectionInfo);
        ArgumentNullException.ThrowIfNull(logger);
        _connectionString = connectionInfo.ConnectionString
            ?? throw new ArgumentException("ConnectionString is required", nameof(connectionInfo));
        _logger = logger;
    }

    public SqlServerInstanceClient(string connectionString, ILogger<SqlServerInstanceClient>? logger = null)
    {
        _connectionString = connectionString
            ?? throw new ArgumentNullException(nameof(connectionString));
        _logger = logger;
    }

    public async Task<bool> CanConnectAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await EnsureConnectionAsync(cancellationToken).ConfigureAwait(false);
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

        var metrics = new InstanceMetrics
        {
            ActiveRequests = await GetMetricAsync(
                "ActiveRequests",
                "SELECT COUNT(*) FROM sys.dm_exec_requests WHERE session_id <> @@SPID;",
                reader => reader.GetInt32(0),
                cancellationToken),
            BlockingSessions = await GetMetricAsync(
                "BlockingSessions",
                "SELECT COUNT(DISTINCT blocking_session_id) FROM sys.dm_exec_requests WHERE blocking_session_id <> 0;",
                reader => reader.GetInt32(0),
                cancellationToken),
            WaitTimeMs = await GetMetricAsync(
                "WaitTimeMs",
                "SELECT ISNULL(SUM(CAST(wait_time AS BIGINT)), 0) FROM sys.dm_exec_requests WHERE session_id <> @@SPID;",
                reader => reader.GetInt64(0),
                cancellationToken),
            CpuPercent = await GetCpuPercentAsync(cancellationToken),
            MemoryPercent = await GetMemoryPercentAsync(cancellationToken)
        };

        metrics.ActiveRequestsDetail = await GetActiveRequestsDetailSafelyAsync(cancellationToken).ConfigureAwait(false);
        return metrics;
    }

    private async Task<T> GetMetricAsync<T>(
        string metricName,
        string query,
        Func<SqlDataReader, T> readValue,
        CancellationToken cancellationToken)
    {
        try
        {
            using var command = new SqlCommand(query, _connection);
            using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
            if (await reader.ReadAsync(cancellationToken).ConfigureAwait(false) && !reader.IsDBNull(0))
            {
                return readValue(reader);
            }
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Failed to read metric {MetricName}. Returning default value.", metricName);
        }

        return default!;
    }

    private async Task<double> GetCpuPercentAsync(CancellationToken cancellationToken)
    {
        const string query = @"
            SELECT ISNULL(CAST(AVG(100 - SystemIdle) AS FLOAT), 0)
            FROM (SELECT TOP 10
                    CAST(record.value('(./Record/SchedulerMonitorEvent/SystemHealth/SystemIdle)[1]', 'INT') AS FLOAT) AS SystemIdle
                  FROM (SELECT CAST(record AS XML) AS record, [timestamp]
                        FROM sys.dm_os_ring_buffers
                        WHERE ring_buffer_type = N'RING_BUFFER_SCHEDULER_MONITOR'
                          AND record LIKE '%<SystemIdle>%') t
                  ORDER BY [timestamp] DESC) idle;";

        try
        {
            using var command = new SqlCommand(query, _connection);
            using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
            if (await reader.ReadAsync(cancellationToken).ConfigureAwait(false) && !reader.IsDBNull(0))
            {
                return reader.GetDouble(0);
            }
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Failed to read CPU percent from sys.dm_os_ring_buffers. Returning 0.");
        }

        return 0;
    }

    private async Task<double> GetMemoryPercentAsync(CancellationToken cancellationToken)
    {
        const string query = @"
            SELECT ISNULL(CAST((pm.physical_memory_in_use_kb / 1024.0) / (sm.total_physical_memory_kb / 1024.0) * 100.0 AS FLOAT), 0)
            FROM sys.dm_os_process_memory pm
            CROSS JOIN sys.dm_os_sys_memory sm;";

        try
        {
            using var command = new SqlCommand(query, _connection);
            using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
            if (await reader.ReadAsync(cancellationToken).ConfigureAwait(false) && !reader.IsDBNull(0))
            {
                return reader.GetDouble(0);
            }
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Failed to read memory percent from DMVs. Returning 0.");
        }

        return 0;
    }

    private async Task<IReadOnlyList<ActiveRequest>> GetActiveRequestsDetailSafelyAsync(CancellationToken cancellationToken)
    {
        try
        {
            return await GetActiveRequestsAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Failed to read active requests detail. Returning empty list.");
            return new List<ActiveRequest>();
        }
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
