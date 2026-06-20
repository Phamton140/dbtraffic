namespace DbTraffic.Shared.Models;

public sealed class InstanceConnectionInfo
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string ConnectionString { get; set; } = string.Empty;
}
