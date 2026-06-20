namespace DbTraffic.Core.Entities;

public sealed class DiscoveredObject
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid InstanceId { get; set; }
    public string SchemaName { get; set; } = string.Empty;
    public string ObjectName { get; set; } = string.Empty;
    public string ObjectType { get; set; } = string.Empty;
    public DateTime DiscoveredAt { get; init; } = DateTime.UtcNow;
}
