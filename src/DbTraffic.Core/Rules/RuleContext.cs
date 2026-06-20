using DbTraffic.Core.Entities;

namespace DbTraffic.Core.Rules;

public sealed class RuleContext
{
    public required Process Process { get; init; }
    public required Instance Instance { get; init; }
    public required DateTime ProposedStartTime { get; init; }
    public required IReadOnlyList<Process> OverlappingProcesses { get; init; }
    public required IReadOnlyList<ProcessObject> ProcessObjects { get; init; }
    public required IReadOnlyDictionary<Guid, IReadOnlyList<ProcessObject>> ObjectsByProcessId { get; init; }
    public InstanceResourceState? ResourceState { get; init; }

    public DateTime ProposedEndTime =>
        ProposedStartTime.AddMinutes(Process.EstimatedDurationMinutes ?? 60);
}

public sealed class InstanceResourceState
{
    public double CpuPercent { get; init; }
    public double MemoryPercent { get; init; }
    public int ActiveRequests { get; init; }
    public int BlockingSessions { get; init; }
}
