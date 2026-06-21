using System.Text;
using Dapper;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace DbTraffic.Infrastructure.Data;

public sealed class DatabaseInitializer
{
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly IHostEnvironment _environment;
    private readonly ILogger<DatabaseInitializer> _logger;

    public DatabaseInitializer(
        IDbConnectionFactory connectionFactory,
        IHostEnvironment environment,
        ILogger<DatabaseInitializer> logger)
    {
        _connectionFactory = connectionFactory;
        _environment = environment;
        _logger = logger;
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
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

    private async Task<bool> SchemaExistsAsync(CancellationToken cancellationToken)
    {
        try
        {
            const string sql = @"
                SELECT COUNT(*)
                FROM sys.tables t
                INNER JOIN sys.schemas s ON t.schema_id = s.schema_id
                WHERE s.name = 'catalog' AND t.name = 'Instances';";

            using var connection = _connectionFactory.CreateConnection();
            var count = await connection.ExecuteScalarAsync<int>(sql);
            return count > 0;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not verify database schema existence. Will attempt to apply schema.");
            return false;
        }
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
