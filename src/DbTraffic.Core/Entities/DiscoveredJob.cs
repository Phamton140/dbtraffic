namespace DbTraffic.Core.Entities;

public sealed class DiscoveredJob
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid InstanceId { get; set; }
    public Guid JobId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool Enabled { get; set; }
    public DateTime DiscoveredAt { get; init; } = DateTime.UtcNow;
    public Guid? AssociatedProcessId { get; set; }
}
