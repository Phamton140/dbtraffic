using System.IO.Pipes;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Testcontainers.MsSql;

namespace DbTraffic.Web.Tests;

public sealed class WebApplicationFactoryFixture : WebApplicationFactory<Program>, IAsyncLifetime
{
    private MsSqlContainer? _container;

    public bool IsAvailable { get; private set; }

    public async Task InitializeAsync()
    {
        if (!IsDockerEngineAvailable())
        {
            IsAvailable = false;
            return;
        }

        try
        {
            _container = new MsSqlBuilder("mcr.microsoft.com/mssql/server:2019-latest").Build();
            await _container.StartAsync();
            IsAvailable = true;
        }
        catch (Exception)
        {
            IsAvailable = false;
        }
    }

    public new async Task DisposeAsync()
    {
        if (_container is not null)
        {
            await _container.DisposeAsync();
        }

        await base.DisposeAsync();
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        if (IsAvailable && _container is not null)
        {
            builder.ConfigureAppConfiguration((_, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:DbTraffic"] = _container.GetConnectionString()
                });
            });
        }

        builder.ConfigureServices(services =>
        {
            var hostedServices = services
                .Where(descriptor => descriptor.ServiceType == typeof(IHostedService))
                .ToList();

            foreach (var descriptor in hostedServices)
            {
                services.Remove(descriptor);
            }
        });

        var host = builder.Build();
        host.StartAsync().GetAwaiter().GetResult();
        return host;
    }

    private static bool IsDockerEngineAvailable()
    {
        try
        {
            if (OperatingSystem.IsWindows())
            {
                using var client = new NamedPipeClientStream(".", "docker_engine", PipeDirection.InOut);
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

[CollectionDefinition("Web integration collection")]
public class WebIntegrationCollection : ICollectionFixture<WebApplicationFactoryFixture>
{
}
