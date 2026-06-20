namespace DbTraffic.Core.Entities;

public sealed class Execution
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid? ProcessId { get; set; }
    public Guid InstanceId { get; set; }
    public string Source { get; set; } = "Manual";
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public int? DurationMinutes { get; set; }
    public string Status { get; set; } = "Running";
    public string? AffectedObjectsJson { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
}
