using DbTraffic.Core.Entities;
using DbTraffic.Core.Repositories;
using DbTraffic.Core.Rules;

namespace DbTraffic.Core.Services;

public sealed class RecommendationService : IRecommendationService
{
    private readonly IProcessRepository _processRepository;
    private readonly IInstanceRepository _instanceRepository;
    private readonly IRiskCalculationService _riskCalculationService;

    public RecommendationService(
        IProcessRepository processRepository,
        IInstanceRepository instanceRepository,
        IRiskCalculationService riskCalculationService)
    {
        _processRepository = processRepository;
        _instanceRepository = instanceRepository;
        _riskCalculationService = riskCalculationService;
    }

    public async Task<IReadOnlyList<RecommendationWindow>> FindWindowsAsync(RecommendationRequest request, CancellationToken cancellationToken = default)
    {
        var process = await _processRepository.GetByIdAsync(request.ProcessId, cancellationToken);
        if (process is null)
        {
            return Array.Empty<RecommendationWindow>();
        }

        var instance = await _instanceRepository.GetByIdAsync(process.InstanceId, cancellationToken);
        if (instance is null)
        {
            return Array.Empty<RecommendationWindow>();
        }

        var durationMinutes = process.EstimatedDurationMinutes ?? 60;
        var allProcessIds = await _processRepository.GetByInstanceIdAsync(instance.Id, cancellationToken);
        var allProcesses = new List<Process>();
        var objectsByProcessId = new Dictionary<Guid, IReadOnlyList<ProcessObject>>();

        foreach (var p in allProcessIds)
        {
            var fullProcess = await _processRepository.GetByIdAsync(p.Id, cancellationToken);
            if (fullProcess is not null)
            {
                allProcesses.Add(fullProcess);
                objectsByProcessId[fullProcess.Id] = fullProcess.Objects;
            }
        }

        var candidates = new List<RecommendationWindow>();
        var current = request.SearchStart;

        while (current.AddMinutes(durationMinutes) <= request.SearchEnd)
        {
            var context = BuildContext(
                process,
                instance,
                current,
                allProcesses,
                objectsByProcessId);

            var assessment = await _riskCalculationService.CalculateAsync(context, cancellationToken);

            candidates.Add(new RecommendationWindow
            {
                StartTime = current,
                EndTime = current.AddMinutes(durationMinutes),
                RiskScore = assessment.TotalScore,
                RiskLevel = assessment.OverallLevel
            });

            current = current.AddMinutes(request.GranularityMinutes);
        }

        return candidates
            .Where(c => c.RiskLevel <= RiskLevel.Medium)
            .OrderBy(c => c.RiskScore)
            .ThenBy(c => c.StartTime)
            .Take(request.MaxRecommendations)
            .ToList();
    }

    private static RuleContext BuildContext(
        Process process,
        Instance instance,
        DateTime proposedStart,
        List<Process> allProcesses,
        Dictionary<Guid, IReadOnlyList<ProcessObject>> objectsByProcessId)
    {
        var proposedEnd = proposedStart.AddMinutes(process.EstimatedDurationMinutes ?? 60);

        var overlappingProcesses = allProcesses
            .Where(p => p.Id != process.Id && p.IsActive)
            .Where(p => p.Schedules.Any(s => s.IsActive && SchedulesOverlap(s, proposedStart, proposedEnd)))
            .ToList();

        return new RuleContext
        {
            Process = process,
            Instance = instance,
            ProposedStartTime = proposedStart,
            OverlappingProcesses = overlappingProcesses,
            ProcessObjects = objectsByProcessId.GetValueOrDefault(process.Id) ?? new List<ProcessObject>(),
            ObjectsByProcessId = objectsByProcessId,
            ResourceState = null
        };
    }

    private static bool SchedulesOverlap(ProcessSchedule schedule, DateTime proposedStart, DateTime proposedEnd)
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
        return scheduleStart < proposedEnd && scheduleEnd > proposedStart;
    }
}
