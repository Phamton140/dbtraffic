using System.Text;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace DbTraffic.Infrastructure.Data;

public sealed class DatabaseInitializer
{
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly IHostEnvironment _environment;
    private readonly IConfiguration _configuration;
    private readonly ILogger<DatabaseInitializer> _logger;

    public DatabaseInitializer(
        IDbConnectionFactory connectionFactory,
        IHostEnvironment environment,
        IConfiguration configuration,
        ILogger<DatabaseInitializer> logger)
    {
        _connectionFactory = connectionFactory;
        _environment = environment;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        var connectionString = _configuration.GetConnectionString("DbTraffic")
            ?? throw new InvalidOperationException("Connection string 'DbTraffic' is not configured.");

        var builder = new SqlConnectionStringBuilder(connectionString);
        var databaseName = builder.InitialCatalog;

        if (string.IsNullOrWhiteSpace(databaseName))
        {
            _logger.LogWarning("The DbTraffic connection string does not specify an InitialCatalog/Database. Skipping automatic database creation.");
        }
        else if (!IsSystemDatabase(databaseName))
        {
            await EnsureDatabaseExistsAsync(connectionString, databaseName, cancellationToken);
        }

        if (await SchemaExistsAsync(cancellationToken))
        {
            _logger.LogInformation("Database schema already exists. Skipping initialization.");
            return;
        }

        _logger.LogInformation("Database schema not found. Applying schema.sql...");

        var schemaPath = FindSchemaPath();
        var script = await File.ReadAllTextAsync(schemaPath, cancellationToken);
        var batches = SplitBatches(script);

        using var connection = _connectionFactory.CreateConnection();
        foreach (var batch in batches)
        {
            await connection.ExecuteAsync(batch);
        }

        _logger.LogInformation("Database schema applied successfully.");
    }

    private static bool IsSystemDatabase(string databaseName)
    {
        return string.Equals(databaseName, "master", StringComparison.OrdinalIgnoreCase)
            || string.Equals(databaseName, "tempdb", StringComparison.OrdinalIgnoreCase)
            || string.Equals(databaseName, "model", StringComparison.OrdinalIgnoreCase)
            || string.Equals(databaseName, "msdb", StringComparison.OrdinalIgnoreCase);
    }

    private async Task EnsureDatabaseExistsAsync(string connectionString, string databaseName, CancellationToken cancellationToken)
    {
        var masterConnectionString = new SqlConnectionStringBuilder(connectionString)
        {
            InitialCatalog = "master"
        }.ConnectionString;

        const string checkDatabaseSql = @"
            SELECT COUNT(*)
            FROM sys.databases
            WHERE name = @DatabaseName;";

        using var masterConnection = new SqlConnection(masterConnectionString);
        var exists = await masterConnection.ExecuteScalarAsync<int>(
            new CommandDefinition(checkDatabaseSql, new { DatabaseName = databaseName }, cancellationToken: cancellationToken));

        if (exists > 0)
        {
            _logger.LogInformation("Database {DatabaseName} already exists.", databaseName);
            return;
        }

        _logger.LogInformation("Creating database {DatabaseName}...", databaseName);

        var createDatabaseSql = $@"
            CREATE DATABASE [{databaseName.Replace("]", "]]")}];";

        await masterConnection.ExecuteAsync(
            new CommandDefinition(createDatabaseSql, cancellationToken: cancellationToken));

        _logger.LogInformation("Database {DatabaseName} created successfully.", databaseName);
    }

    private async Task<bool> SchemaExistsAsync(CancellationToken cancellationToken)
    {
        const string sql = @"
            SELECT COUNT(*)
            FROM sys.tables t
            INNER JOIN sys.schemas s ON t.schema_id = s.schema_id
            WHERE s.name = 'catalog' AND t.name = 'Instances';";

        using var connection = _connectionFactory.CreateConnection();
        var count = await connection.ExecuteScalarAsync<int>(
            new CommandDefinition(sql, cancellationToken: cancellationToken));
        return count > 0;
    }

    private string FindSchemaPath()
    {
        var contentRoot = _environment.ContentRootPath;
        var candidates = new[]
        {
            Path.Combine(contentRoot, "..", "..", "database", "schema.sql"),
            Path.Combine(contentRoot, "..", "database", "schema.sql"),
            Path.Combine(contentRoot, "database", "schema.sql"),
            Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "database", "schema.sql"),
            Path.Combine(Directory.GetCurrentDirectory(), "..", "database", "schema.sql"),
            Path.Combine(Directory.GetCurrentDirectory(), "database", "schema.sql")
        };

        foreach (var candidate in candidates)
        {
            var fullPath = Path.GetFullPath(candidate);
            if (File.Exists(fullPath))
            {
                return fullPath;
            }
        }

        throw new FileNotFoundException("Could not find database/schema.sql. Ensure the database schema is applied manually.");
    }

    private static IReadOnlyList<string> SplitBatches(string script)
    {
        var lines = script.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
        var batches = new List<StringBuilder> { new() };

        foreach (var rawLine in lines)
        {
            var line = rawLine.Trim();
            if (line.Equals("GO", StringComparison.OrdinalIgnoreCase))
            {
                batches.Add(new StringBuilder());
            }
            else
            {
                batches[^1].AppendLine(rawLine);
            }
        }

        return batches
            .Select(batch => batch.ToString().Trim())
            .Where(batch => !string.IsNullOrWhiteSpace(batch))
            .ToList();
    }
}
