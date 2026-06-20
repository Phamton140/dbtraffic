using DbTraffic.Core.Enums;

namespace DbTraffic.Core.Entities;

public sealed class ProcessObject
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid ProcessId { get; set; }
    public string SchemaName { get; set; } = string.Empty;
    public string ObjectName { get; set; } = string.Empty;
    public ObjectType ObjectType { get; set; }
    public bool IsCritical { get; set; }
    public ObjectAccessType? AccessType { get; set; }
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
}
