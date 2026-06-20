using DbTraffic.Core.Exceptions;

namespace DbTraffic.Core.Entities;

public sealed class Instance
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string ConnectionString { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public void Touch()
    {
        UpdatedAt = DateTime.UtcNow;
    }

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(Name))
        {
            throw new DomainException("Instance name is required.");
        }

        if (string.IsNullOrWhiteSpace(ConnectionString))
        {
            throw new DomainException("Connection string is required.");
        }
    }
}
