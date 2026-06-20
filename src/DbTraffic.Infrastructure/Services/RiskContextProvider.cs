using DbTraffic.Core.Entities;
using DbTraffic.Core.Repositories;
using DbTraffic.Core.Rules;
using DbTraffic.Core.Services;
using DbTraffic.Infrastructure.SqlServer;

namespace DbTraffic.Infrastructure.Services;

public sealed class RiskContextProvider : IRiskContextProvider
{
    private readonly IProcessRepository _processRepository;
    private readonly IInstanceRepository _instanceRepository;
    private readonly ISqlServerInstanceClient _instanceClient;

    public RiskContextProvider(
        IProcessRepository processRepository,
        IInstanceRepository instanceRepository,
        ISqlServerInstanceClient instanceClient)
    {
        _processRepository = processRepository;
        _instanceRepository = instanceRepository;
        _instanceClient = instanceClient;
    }

    public async Task<RuleContext?> BuildContextAsync(Guid processId, DateTime proposedStartTime, CancellationToken cancellationToken = default)
    {
        var process = await _processRepository.GetByIdAsync(processId, cancellationToken);
        if (process is null)
        {
            return null;
        }

        var instance = await _instanceRepository.GetByIdAsync(process.InstanceId, cancellationToken);
        if (instance is null)
        {
            return null;
        }

        var processIds = await _processRepository.GetByInstanceIdAsync(instance.Id, cancellationToken);
        var allProcesses = new List<Process>();
        foreach (var p in processIds)
        {
            var fullProcess = await _processRepository.GetByIdAsync(p.Id, cancellationToken);
            if (fullProcess is not null)
            {
                allProcesses.Add(fullProcess);
            }
        }

        var overlappingProcesses = GetOverlappingProcesses(process, proposedStartTime, allProcesses).ToList();

        var objectsByProcessId = new Dictionary<Guid, IReadOnlyList<ProcessObject>>();
        foreach (var p in overlappingProcesses.Concat(new[] { process }))
        {
            var fullProcess = await _processRepository.GetByIdAsync(p.Id, cancellationToken);
            objectsByProcessId[p.Id] = fullProcess?.Objects ?? new List<ProcessObject>();
        }

        var resourceState = await GetResourceStateAsync(cancellationToken);

        return new RuleContext
        {
            Process = process,
            Instance = instance,
            ProposedStartTime = proposedStartTime,
            OverlappingProcesses = overlappingProcesses,
            ProcessObjects = objectsByProcessId[process.Id],
            ObjectsByProcessId = objectsByProcessId,
            ResourceState = resourceState
        };
    }

    private IEnumerable<Process> GetOverlappingProcesses(Process current, DateTime proposedStart, IReadOnlyList<Process> allProcesses)
    {
        var proposedEnd = proposedStart.AddMinutes(current.EstimatedDurationMinutes ?? 60);

        foreach (var other in allProcesses)
        {
            if (other.Id == current.Id)
            {
                continue;
            }

            if (!other.IsActive)
            {
                continue;
            }

            // For MVP, we only evaluate schedule overlap if the other process has explicit schedules.
            // If no schedules are defined, we assume it could run at any time within its preferred window.
            foreach (var schedule in other.Schedules.Where(s => s.IsActive))
            {
                DateTime scheduleStart;
                if (schedule.DayOfWeek.HasValue)
                {
                    var daysDiff = (int)schedule.DayOfWeek.Value - (int)proposedStart.DayOfWeek;
                    scheduleStart = proposedStart.Date.AddDays(daysDiff).Add(schedule.StartTime);
                }
                else
                {
                    scheduleStart = proposedStart.Date.Add(schedule.StartTime);
                }

                var scheduleEnd = scheduleStart.AddMinutes(schedule.DurationMinutes);

                if (scheduleStart < proposedEnd && scheduleEnd > proposedStart)
                {
                    yield return other;
                    break;
                }
            }
        }
    }

    private async Task<InstanceResourceState?> GetResourceStateAsync(CancellationToken cancellationToken)
    {
        try
        {
            var requests = await _instanceClient.GetActiveRequestsAsync(cancellationToken);
            return new InstanceResourceState
            {
                CpuPercent = 0,
                MemoryPercent = 0,
                ActiveRequests = requests.Count,
                BlockingSessions = 0
            };
        }
        catch
        {
            return null;
        }
    }
}
