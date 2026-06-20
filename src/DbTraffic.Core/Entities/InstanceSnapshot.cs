namespace DbTraffic.Core.Entities;

public sealed class InstanceSnapshot
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid InstanceId { get; set; }
    public DateTime CapturedAt { get; init; } = DateTime.UtcNow;
    public decimal? CpuPercent { get; set; }
    public decimal? MemoryPercent { get; set; }
    public int? ActiveRequests { get; set; }
    public int? BlockingSessions { get; set; }
    public long? WaitTimeMs { get; set; }
    public string? SnapshotJson { get; set; }
}
