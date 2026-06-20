namespace DbTraffic.Core.Entities;

public sealed class ProcessSchedule
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid ProcessId { get; set; }
    public DayOfWeek? DayOfWeek { get; set; }
    public TimeSpan StartTime { get; set; }
    public int DurationMinutes { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
}
