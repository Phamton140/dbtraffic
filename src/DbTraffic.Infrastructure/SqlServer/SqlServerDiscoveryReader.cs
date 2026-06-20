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
