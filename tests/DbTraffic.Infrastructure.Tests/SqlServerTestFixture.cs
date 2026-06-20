using System.Data;
using System.Text;
using Dapper;
using DbTraffic.Infrastructure.Data;
using DotNet.Testcontainers.Builders;
using Testcontainers.MsSql;

namespace DbTraffic.Infrastructure.Tests;

public sealed class SqlServerTestFixture : IAsyncLifetime
{
    private MsSqlContainer? _container;

    public bool IsAvailable => _container is not null;

    public string ConnectionString =>
        _container?.GetConnectionString()
        ?? throw new InvalidOperationException("Testcontainer is not available.");

    public IDbConnectionFactory CreateConnectionFactory() =>
        new SqlConnectionFactory(ConnectionString);

    public async Task InitializeAsync()
    {
        if (!ShouldRunIntegrationTests())
        {
            return;
        }

        _container = new MsSqlBuilder()
            .WithImage("mcr.microsoft.com/mssql/server:2019-latest")
            .WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(1433))
            .Build();

        try
        {
            await _container.StartAsync();
            await ApplySchemaWithRetryAsync();
        }
        catch (Exception)
        {
            var (stdout, stderr) = await _container.GetLogsAsync();
            Console.WriteLine("=== SQL Server container stdout ===");
            Console.WriteLine(stdout);
            Console.WriteLine("=== SQL Server container stderr ===");
            Console.WriteLine(stderr);
            throw;
        }
    }

    public async Task DisposeAsync()
    {
        if (_container is not null)
        {
            await _container.DisposeAsync();
        }
    }

    private static bool ShouldRunIntegrationTests()
    {
        var value = Environment.GetEnvironmentVariable("RUN_INTEGRATION_TESTS");
        return bool.TryParse(value, out var result) && result;
    }

    private async Task ApplySchemaWithRetryAsync()
    {
        const int maxAttempts = 30;
        var delay = TimeSpan.FromSeconds(1);
        Exception? lastException = null;

        var scriptPath = FindSchemaPath();
        var script = await File.ReadAllTextAsync(scriptPath);
        var batches = SplitBatches(script);

        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                using var connection = CreateConnectionFactory().CreateConnection();
                foreach (var batch in batches)
                {
                    await connection.ExecuteAsync(batch);
                }

                return;
            }
            catch (Exception ex)
            {
                lastException = ex;
                if (attempt < maxAttempts)
                {
                    await Task.Delay(delay);
                }
            }
        }

        throw new InvalidOperationException(
            $"Could not apply schema after {maxAttempts} attempts.",
            lastException);
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

    private static string FindSchemaPath()
    {
        var currentDirectory = Directory.GetCurrentDirectory();
        while (!string.IsNullOrEmpty(currentDirectory))
        {
            var candidate = Path.Combine(currentDirectory, "database", "schema.sql");
            if (File.Exists(candidate))
            {
                return candidate;
            }
            currentDirectory = Directory.GetParent(currentDirectory)?.FullName;
        }
        throw new FileNotFoundException("Could not find database/schema.sql");
    }
}

[CollectionDefinition("SqlServer collection")]
public class SqlServerCollection : ICollectionFixture<SqlServerTestFixture>
{
}
