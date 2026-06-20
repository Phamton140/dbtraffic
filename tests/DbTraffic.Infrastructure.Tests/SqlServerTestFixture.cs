using System.Data;
using Dapper;
using DbTraffic.Infrastructure.Data;
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
            .WithImage("mcr.microsoft.com/mssql/server:2022-latest")
            .Build();

        await _container.StartAsync();
        await ApplySchemaAsync();
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

    private async Task ApplySchemaAsync()
    {
        var scriptPath = FindSchemaPath();
        var script = await File.ReadAllTextAsync(scriptPath);
        using var connection = CreateConnectionFactory().CreateConnection();
        await connection.ExecuteAsync(script);
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
