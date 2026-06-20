using DbTraffic.Core.Enums;
using DbTraffic.Core.Exceptions;

namespace DbTraffic.Core.Entities;

public sealed class Process
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid InstanceId { get; set; }
    public string Name { get; set; } = string.Empty;
    public ProcessType ProcessType { get; set; }
    public string? Description { get; set; }
    public int? EstimatedDurationMinutes { get; set; }
    public TimeSpan? PreferredWindowStart { get; set; }
    public TimeSpan? PreferredWindowEnd { get; set; }
    public IntensityLevel CpuIntensity { get; set; } = IntensityLevel.Low;
    public IntensityLevel IoIntensity { get; set; } = IntensityLevel.Low;
    public IntensityLevel MemoryIntensity { get; set; } = IntensityLevel.Low;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public List<ProcessObject> Objects { get; init; } = new();
    public List<ProcessSchedule> Schedules { get; init; } = new();

    public void Touch()
    {
        UpdatedAt = DateTime.UtcNow;
    }

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(Name))
        {
            throw new DomainException("Process name is required.");
        }

        if (InstanceId == Guid.Empty)
        {
            throw new DomainException("Instance is required.");
        }

        if (EstimatedDurationMinutes.HasValue && EstimatedDurationMinutes.Value <= 0)
        {
            throw new DomainException("Estimated duration must be greater than zero.");
        }

        if (PreferredWindowStart.HasValue && PreferredWindowEnd.HasValue && PreferredWindowEnd <= PreferredWindowStart)
        {
            throw new DomainException("Preferred window end must be after start.");
        }
    }
}
