using DotNet.Testcontainers.Builders;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Testcontainers.MsSql;

namespace DbTraffic.E2ETests;

public sealed class DbTrafficWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private MsSqlContainer? _container;

    public string BaseUrl { get; private set; } = string.Empty;

    public bool IsAvailable { get; private set; }

    public async Task InitializeAsync()
    {
        if (!ShouldRunE2ETests())
        {
            IsAvailable = false;
            return;
        }

        _container = new MsSqlBuilder()
            .WithImage("mcr.microsoft.com/mssql/server:2019-latest")
            .WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(1433))
            .Build();

        try
        {
            await _container.StartAsync();
            IsAvailable = true;

            // Force the WebApplicationFactory to create the host and assign BaseUrl
            // before any test reads it.
            using var client = CreateDefaultClient();
        }
        catch (Exception)
        {
            IsAvailable = false;
        }
    }

    Task IAsyncLifetime.DisposeAsync()
    {
        return DisposeAsync().AsTask();
    }

    public override async ValueTask DisposeAsync()
    {
        if (_container is not null)
        {
            await _container.DisposeAsync();
        }

        await base.DisposeAsync();
    }

    private static bool ShouldRunE2ETests()
    {
        var value = Environment.GetEnvironmentVariable("RUN_E2E_TESTS");
        return bool.TryParse(value, out var result) && result;
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        if (_container is null)
        {
            throw new InvalidOperationException("SQL Server container is not initialized.");
        }

        var port = GetFreePort();
        BaseUrl = $"http://127.0.0.1:{port}";

        builder.ConfigureWebHost(webHostBuilder =>
        {
            webHostBuilder.UseKestrel();
            webHostBuilder.UseUrls(BaseUrl);
        });

        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["DbTraffic:DemoInstance:ConnectionString"] = _container.GetConnectionString(),
                ["DbTraffic:DemoInstance:Name"] = "E2E",
                ["DbTraffic:Discovery:IntervalMinutes"] = "60",
                ["DbTraffic:Monitoring:IntervalMinutes"] = "5"
            });
        });

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

        var server = host.Services.GetRequiredService<IServer>();
        var addresses = server.Features.Get<IServerAddressesFeature>()?.Addresses;
        if (addresses is not null && addresses.Any())
        {
            BaseUrl = NormalizeLocalhost(addresses.First());
        }

        return host;
    }

    private static string NormalizeLocalhost(string address)
    {
        // Playwright cannot navigate to http://[::]:port, so normalize to 127.0.0.1
        if (address.Contains("[::]"))
        {
            var port = new Uri(address).Port;
            return $"http://127.0.0.1:{port}";
        }

        return address;
    }

    private static int GetFreePort()
    {
        using var listener = new System.Net.Sockets.TcpListener(System.Net.IPAddress.Loopback, 0);
        listener.Start();
        var port = ((System.Net.IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }
}
