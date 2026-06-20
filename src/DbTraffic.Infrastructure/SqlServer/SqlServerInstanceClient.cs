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
