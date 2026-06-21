using System.Data;
using Dapper;
using DbTraffic.Infrastructure.Data;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Hosting.Internal;
using Microsoft.Extensions.Logging.Abstractions;
using Testcontainers.MsSql;

namespace DbTraffic.Infrastructure.Tests;

public sealed class DatabaseInitializerTests : IAsyncLifetime
{
    private MsSqlContainer? _container;

    public async Task InitializeAsync()
    {
        if (!IsDockerEngineAvailable())
        {
            return;
        }

        _container = new MsSqlBuilder().WithImage("mcr.microsoft.com/mssql/server:2019-latest").Build();
        await _container.StartAsync();
    }

    public async Task DisposeAsync()
    {
        if (_container is not null)
        {
            await _container.DisposeAsync();
        }
    }

    [Fact]
    public async Task InitializeAsync_DatabaseDoesNotExist_CreatesDatabaseAndSchema()
    {
        if (_container is null)
        {
            return;
        }

        var databaseName = $"DbTrafficTest_{Guid.NewGuid():N}";
        var connectionString = new SqlConnectionStringBuilder(_container.GetConnectionString())
        {
            InitialCatalog = databaseName
        }.ConnectionString;

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DbTraffic"] = connectionString
            })
            .Build();

        var connectionFactory = new SqlConnectionFactory(configuration);
        var environment = new HostingEnvironment { ContentRootPath = AppContext.BaseDirectory };
        var initializer = new DatabaseInitializer(
            connectionFactory,
            environment,
            configuration,
            NullLogger<DatabaseInitializer>.Instance);

        await initializer.InitializeAsync();

        using var connection = connectionFactory.CreateConnection();
        var tableExists = await connection.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM sys.tables t INNER JOIN sys.schemas s ON t.schema_id = s.schema_id WHERE s.name = 'catalog' AND t.name = 'Instances';");

        Assert.Equal(1, tableExists);
    }

    [Fact]
    public async Task InitializeAsync_DatabaseAlreadyExistsWithSchema_DoesNotFail()
    {
        if (_container is null)
        {
            return;
        }

        var databaseName = $"DbTrafficTest_{Guid.NewGuid():N}";
        var connectionString = new SqlConnectionStringBuilder(_container.GetConnectionString())
        {
            InitialCatalog = databaseName
        }.ConnectionString;

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DbTraffic"] = connectionString
            })
            .Build();

        var connectionFactory = new SqlConnectionFactory(configuration);
        var environment = new HostingEnvironment { ContentRootPath = AppContext.BaseDirectory };
        var initializer = new DatabaseInitializer(
            connectionFactory,
            environment,
            configuration,
            NullLogger<DatabaseInitializer>.Instance);

        await initializer.InitializeAsync();
        await initializer.InitializeAsync();

        using var connection = connectionFactory.CreateConnection();
        var tableExists = await connection.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM sys.tables t INNER JOIN sys.schemas s ON t.schema_id = s.schema_id WHERE s.name = 'catalog' AND t.name = 'Instances';");

        Assert.Equal(1, tableExists);
    }

    private static bool IsDockerEngineAvailable()
    {
        try
        {
            if (OperatingSystem.IsWindows())
            {
                using var client = new System.IO.Pipes.NamedPipeClientStream(".", "docker_engine", System.IO.Pipes.PipeDirection.InOut);
                client.Connect(500);
                return true;
            }

            return File.Exists("/var/run/docker.sock");
        }
        catch
        {
            return false;
        }
    }
}
