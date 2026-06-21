namespace DbTraffic.Core.Entities;

public sealed class DiscoveredJobSchedule
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid DiscoveredJobId { get; set; }
    public int ScheduleId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int FrequencyType { get; set; }
    public int FrequencyInterval { get; set; }
    public int FrequencySubdayType { get; set; }
    public int FrequencySubdayInterval { get; set; }
    public int FrequencyRelativeInterval { get; set; }
    public int FrequencyRecurrenceFactor { get; set; }
    public TimeSpan ActiveStartTime { get; set; }
    public TimeSpan ActiveEndTime { get; set; }
    public string Description { get; set; } = string.Empty;
    public DateTime DiscoveredAt { get; init; } = DateTime.UtcNow;
}
