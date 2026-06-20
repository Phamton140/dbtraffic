namespace DbTraffic.Infrastructure.Monitoring;

public sealed class MonitoringWorkerOptions
{
    public TimeSpan Interval { get; set; } = TimeSpan.FromMinutes(5);
    public TimeSpan Retention { get; set; } = TimeSpan.FromDays(7);
}
