using DbTraffic.Core.Repositories;
using DbTraffic.Core.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DbTraffic.Infrastructure.Monitoring;

public sealed class MonitoringWorker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<MonitoringWorker> _logger;
    private readonly TimeSpan _interval;
    private readonly TimeSpan _retention;

    public MonitoringWorker(
        IServiceProvider serviceProvider,
        ILogger<MonitoringWorker> logger,
        IOptions<MonitoringWorkerOptions>? options = null)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _interval = options?.Value.Interval ?? TimeSpan.FromMinutes(5);
        _retention = options?.Value.Retention ?? TimeSpan.FromDays(7);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Monitoring worker started with interval {Interval}.", _interval);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RunMonitoringAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Monitoring worker iteration failed.");
            }

            await Task.Delay(_interval, stoppingToken);
        }
    }

    private async Task RunMonitoringAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var instanceRepository = scope.ServiceProvider.GetRequiredService<IInstanceRepository>();
        var monitoringService = scope.ServiceProvider.GetRequiredService<IMonitoringService>();
        var snapshotRepository = scope.ServiceProvider.GetRequiredService<IInstanceSnapshotRepository>();

        var instances = await instanceRepository.GetAllAsync(cancellationToken);
        foreach (var instance in instances)
        {
            try
            {
                await monitoringService.CaptureSnapshotAsync(instance.Id, cancellationToken);
                _logger.LogInformation("Captured snapshot for instance {InstanceName}.", instance.Name);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to capture snapshot for instance {InstanceName}.", instance.Name);
            }
        }

        try
        {
            await snapshotRepository.DeleteOldAsync(_retention, cancellationToken);
            _logger.LogInformation("Deleted snapshots older than {Retention}.", _retention);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete old snapshots.");
        }
    }
}
