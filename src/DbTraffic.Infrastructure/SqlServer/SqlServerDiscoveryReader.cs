using DbTraffic.Core.Entities;
using DbTraffic.Shared.Models;
using Microsoft.Data.SqlClient;

namespace DbTraffic.Infrastructure.SqlServer;

public sealed class SqlServerDiscoveryReader : ISqlServerDiscoveryReader, IAsyncDisposable
{
    private readonly string _connectionString;
    private SqlConnection? _connection;

    public SqlServerDiscoveryReader(string connectionString)
    {
        _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
    }

    public SqlServerDiscoveryReader(InstanceConnectionInfo connectionInfo)
    {
        ArgumentNullException.ThrowIfNull(connectionInfo);
        _connectionString = connectionInfo.ConnectionString
            ?? throw new ArgumentException("ConnectionString is required", nameof(connectionInfo));
    }

    public async Task<IReadOnlyList<DiscoveredJob>> GetJobsAsync(CancellationToken cancellationToken = default)
    {
        await EnsureConnectionAsync(cancellationToken).ConfigureAwait(false);

        var jobs = await ReadJobsAsync(cancellationToken).ConfigureAwait(false);
        var schedules = await ReadJobSchedulesAsync(cancellationToken).ConfigureAwait(false);
        var durations = await ReadJobDurationsAsync(cancellationToken).ConfigureAwait(false);
        var lastRuns = await ReadJobLastRunsAsync(cancellationToken).ConfigureAwait(false);

        foreach (var job in jobs)
        {
            if (schedules.TryGetValue(job.JobId, out var jobSchedules))
            {
                job.Schedules = jobSchedules;
            }

            if (durations.TryGetValue(job.JobId, out var durationMinutes))
            {
                job.EstimatedDurationMinutes = durationMinutes;
            }

            if (lastRuns.TryGetValue(job.JobId, out var lastRunInfo))
            {
                job.LastRunDate = lastRunInfo.LastRunDate;
            }
        }

        return jobs;
    }

    private async Task<List<DiscoveredJob>> ReadJobsAsync(CancellationToken cancellationToken)
    {
        const string query = @"
            SELECT
                job_id AS JobId,
                name AS Name,
                description AS Description,
                enabled AS Enabled
            FROM msdb.dbo.sysjobs
            ORDER BY name;";

        var jobs = new List<DiscoveredJob>();

        using var command = new SqlCommand(query, _connection);
        using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);

        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            jobs.Add(new DiscoveredJob
            {
                JobId = reader.GetGuid(0),
                Name = reader.IsDBNull(1) ? string.Empty : reader.GetString(1),
                Description = reader.IsDBNull(2) ? null : reader.GetString(2),
                Enabled = reader.GetByte(3) != 0
            });
        }

        return jobs;
    }

    private async Task<Dictionary<Guid, List<DiscoveredJobSchedule>>> ReadJobSchedulesAsync(CancellationToken cancellationToken)
    {
        const string query = @"
            SELECT
                j.job_id AS JobId,
                s.schedule_id AS ScheduleId,
                s.name AS ScheduleName,
                s.freq_type AS FrequencyType,
                s.freq_interval AS FrequencyInterval,
                s.freq_subday_type AS FrequencySubdayType,
                s.freq_subday_interval AS FrequencySubdayInterval,
                s.freq_relative_interval AS FrequencyRelativeInterval,
                s.freq_recurrence_factor AS FrequencyRecurrenceFactor,
                s.active_start_time AS ActiveStartTime,
                s.active_end_time AS ActiveEndTime
            FROM msdb.dbo.sysjobs j
            INNER JOIN msdb.dbo.sysjobschedules js ON j.job_id = js.job_id
            INNER JOIN msdb.dbo.sysschedules s ON js.schedule_id = s.schedule_id
            ORDER BY j.job_id, s.schedule_id;";

        var schedules = new Dictionary<Guid, List<DiscoveredJobSchedule>>();

        using var command = new SqlCommand(query, _connection);
        using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);

        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            var jobId = reader.GetGuid(0);
            var schedule = new DiscoveredJobSchedule
            {
                ScheduleId = reader.GetInt32(1),
                Name = reader.IsDBNull(2) ? string.Empty : reader.GetString(2),
                FrequencyType = reader.GetInt32(3),
                FrequencyInterval = reader.GetInt32(4),
                FrequencySubdayType = reader.GetInt32(5),
                FrequencySubdayInterval = reader.GetInt32(6),
                FrequencyRelativeInterval = reader.GetInt32(7),
                FrequencyRecurrenceFactor = reader.GetInt32(8),
                ActiveStartTime = ParseTime(reader.GetInt32(9)),
                ActiveEndTime = ParseTime(reader.GetInt32(10)),
                Description = BuildScheduleDescription(
                    reader.GetInt32(3),
                    reader.GetInt32(4),
                    reader.GetInt32(5),
                    reader.GetInt32(6),
                    reader.GetInt32(7),
                    reader.GetInt32(8),
                    ParseTime(reader.GetInt32(9)),
                    ParseTime(reader.GetInt32(10)))
            };

            if (!schedules.TryGetValue(jobId, out var list))
            {
                list = new List<DiscoveredJobSchedule>();
                schedules[jobId] = list;
            }

            list.Add(schedule);
        }

        return schedules;
    }

    private async Task<Dictionary<Guid, int>> ReadJobDurationsAsync(CancellationToken cancellationToken)
    {
        const string query = @"
            SELECT
                job_id AS JobId,
                AVG(run_duration) AS AvgDuration
            FROM msdb.dbo.sysjobhistory
            WHERE step_id = 0
              AND run_status = 1
              AND run_duration IS NOT NULL
            GROUP BY job_id;";

        var durations = new Dictionary<Guid, int>();

        using var command = new SqlCommand(query, _connection);
        using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);

        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            var jobId = reader.GetGuid(0);
            var avgDurationRaw = reader.IsDBNull(1) ? 0 : reader.GetInt32(1);
            durations[jobId] = ParseDuration(avgDurationRaw);
        }

        return durations;
    }

    private async Task<Dictionary<Guid, LastRunInfo>> ReadJobLastRunsAsync(CancellationToken cancellationToken)
    {
        const string query = @"
            SELECT
                job_id AS JobId,
                MAX(run_date) AS LastRunDate,
                MAX(run_time) AS LastRunTime
            FROM msdb.dbo.sysjobhistory
            WHERE step_id = 0
            GROUP BY job_id;";

        var lastRuns = new Dictionary<Guid, LastRunInfo>();

        using var command = new SqlCommand(query, _connection);
        using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);

        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            var jobId = reader.GetGuid(0);
            var runDate = reader.IsDBNull(1) ? 0 : reader.GetInt32(1);
            var runTime = reader.IsDBNull(2) ? 0 : reader.GetInt32(2);

            if (runDate > 0)
            {
                lastRuns[jobId] = new LastRunInfo(
                    ParseDateTime(runDate, runTime));
            }
        }

        return lastRuns;
    }

    public async Task<IReadOnlyList<DiscoveredObject>> GetObjectsAsync(CancellationToken cancellationToken = default)
    {
        await EnsureConnectionAsync(cancellationToken).ConfigureAwait(false);

        const string query = @"
            SELECT
                s.name AS SchemaName,
                o.name AS ObjectName,
                o.type_desc AS ObjectType
            FROM sys.objects o
            INNER JOIN sys.schemas s ON o.schema_id = s.schema_id
            WHERE o.type IN ('U', 'V', 'P', 'FN', 'IF', 'TF')
              AND o.is_ms_shipped = 0
            ORDER BY s.name, o.name;";

        var objects = new List<DiscoveredObject>();

        using var command = new SqlCommand(query, _connection);
        using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);

        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            objects.Add(new DiscoveredObject
            {
                SchemaName = reader.IsDBNull(0) ? string.Empty : reader.GetString(0),
                ObjectName = reader.IsDBNull(1) ? string.Empty : reader.GetString(1),
                ObjectType = reader.IsDBNull(2) ? string.Empty : reader.GetString(2)
            });
        }

        return objects;
    }

    private static TimeSpan ParseTime(int hhmmss)
    {
        var hours = hhmmss / 10000;
        var minutes = (hhmmss % 10000) / 100;
        var seconds = hhmmss % 100;
        return new TimeSpan(hours, minutes, seconds);
    }

    private static int ParseDuration(int rawDuration)
    {
        // run_duration is stored as HHMMSS
        if (rawDuration <= 0) return 0;

        var hours = rawDuration / 10000;
        var minutes = (rawDuration % 10000) / 100;
        var seconds = rawDuration % 100;
        var totalMinutes = hours * 60 + minutes;
        if (seconds > 0 || totalMinutes == 0)
        {
            totalMinutes = Math.Max(1, totalMinutes + 1);
        }

        return totalMinutes;
    }

    private static DateTime ParseDateTime(int runDate, int runTime)
    {
        var year = runDate / 10000;
        var month = (runDate % 10000) / 100;
        var day = runDate % 100;
        var hours = runTime / 10000;
        var minutes = (runTime % 10000) / 100;
        var seconds = runTime % 100;
        return new DateTime(year, month, day, hours, minutes, seconds, DateTimeKind.Unspecified);
    }

    private static string BuildScheduleDescription(
        int freqType,
        int freqInterval,
        int freqSubdayType,
        int freqSubdayInterval,
        int freqRelativeInterval,
        int freqRecurrenceFactor,
        TimeSpan activeStartTime,
        TimeSpan activeEndTime)
    {
        var startTime = activeStartTime.ToString(@"hh\:mm");
        var endTime = activeEndTime.ToString(@"hh\:mm");

        string frequency = freqType switch
        {
            1 => "Una sola vez",
            4 => freqInterval == 1 ? "Diariamente" : $"Cada {freqInterval} días",
            8 => $"Semanalmente los {FormatWeekdays(freqInterval)}",
            16 => $"Mensualmente el día {freqInterval}",
            32 => $"Mensualmente {FormatRelativeInterval(freqRelativeInterval)} {FormatWeekday(freqInterval)}",
            64 => "Al iniciar SQL Server Agent",
            128 => "Cuando las CPUs están inactivas",
            _ => $"Frecuencia desconocida ({freqType})"
        };

        string subday = freqSubdayType switch
        {
            2 => $" cada {freqSubdayInterval} segundos",
            4 => $" cada {freqSubdayInterval} minutos",
            8 => $" cada {freqSubdayInterval} horas",
            _ => string.Empty
        };

        var description = $"{frequency}{subday} a las {startTime}";
        if (activeEndTime != TimeSpan.Zero && activeEndTime != new TimeSpan(23, 59, 59))
        {
            description += $" (hasta {endTime})";
        }

        return description;
    }

    private static string FormatWeekdays(int mask)
    {
        var days = new List<string>();
        if ((mask & 1) != 0) days.Add("dom");
        if ((mask & 2) != 0) days.Add("lun");
        if ((mask & 4) != 0) days.Add("mar");
        if ((mask & 8) != 0) days.Add("mié");
        if ((mask & 16) != 0) days.Add("jue");
        if ((mask & 32) != 0) days.Add("vie");
        if ((mask & 64) != 0) days.Add("sáb");
        return days.Count > 0 ? string.Join(", ", days) : "sin días";
    }

    private static string FormatWeekday(int day)
    {
        return day switch
        {
            1 => "domingo",
            2 => "lunes",
            3 => "martes",
            4 => "miércoles",
            5 => "jueves",
            6 => "viernes",
            7 => "sábado",
            _ => $"día {day}"
        };
    }

    private static string FormatRelativeInterval(int relativeInterval)
    {
        return relativeInterval switch
        {
            1 => "primer",
            2 => "segundo",
            4 => "tercer",
            8 => "cuarto",
            16 => "último",
            _ => $"relativo {relativeInterval}"
        };
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

    private sealed record LastRunInfo(DateTime LastRunDate);
}
