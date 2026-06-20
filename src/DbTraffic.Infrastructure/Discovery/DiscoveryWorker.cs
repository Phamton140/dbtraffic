using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DbTraffic.Infrastructure.Discovery;

public sealed class DiscoveryWorker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<DiscoveryWorker> _logger;
    private readonly TimeSpan _interval;

    public DiscoveryWorker(
        IServiceProvider serviceProvider,
        ILogger<DiscoveryWorker> logger,
        IOptions<DiscoveryWorkerOptions>? options = null)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _interval = options?.Value.Interval ?? TimeSpan.FromHours(1);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Discovery worker started with interval {Interval}.", _interval);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RunDiscoveryAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Discovery worker iteration failed.");
            }

            await Task.Delay(_interval, stoppingToken);
        }
    }

    private async Task RunDiscoveryAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var discoveryService = scope.ServiceProvider.GetRequiredService<DiscoveryService>();
        await discoveryService.DiscoverAllActiveInstancesAsync(cancellationToken);
    }
}

public sealed class DiscoveryWorkerOptions
{
    public TimeSpan Interval { get; set; } = TimeSpan.FromHours(1);
}
