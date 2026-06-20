using DbTraffic.Core.Entities;
using DbTraffic.Core.Repositories;
using DbTraffic.Core.Services;
using DbTraffic.Infrastructure.SqlServer;

namespace DbTraffic.Infrastructure.Services;

public sealed class ExecutionService : IExecutionService
{
    private readonly IExecutionRepository _executionRepository;
    private readonly IProcessRepository _processRepository;
    private readonly IInstanceRepository _instanceRepository;

    public ExecutionService(
        IExecutionRepository executionRepository,
        IProcessRepository processRepository,
        IInstanceRepository instanceRepository)
    {
        _executionRepository = executionRepository;
        _processRepository = processRepository;
        _instanceRepository = instanceRepository;
    }

    public Task<IReadOnlyList<Execution>> GetAllAsync(CancellationToken cancellationToken = default)
        => _executionRepository.GetAllAsync(cancellationToken);

    public Task<IReadOnlyList<Execution>> GetByInstanceIdAsync(Guid instanceId, CancellationToken cancellationToken = default)
        => _executionRepository.GetByInstanceIdAsync(instanceId, cancellationToken);

    public Task<IReadOnlyList<Execution>> GetByProcessIdAsync(Guid processId, CancellationToken cancellationToken = default)
        => _executionRepository.GetByProcessIdAsync(processId, cancellationToken);

    public Task<Execution?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => _executionRepository.GetByIdAsync(id, cancellationToken);

    public async Task<Execution> CreateAsync(Execution execution, CancellationToken cancellationToken = default)
    {
        ValidateExecution(execution);
        return await _executionRepository.CreateAsync(execution, cancellationToken);
    }

    public async Task<Execution> CompleteAsync(Guid id, DateTime completedAt, string status, CancellationToken cancellationToken = default)
    {
        var execution = await _executionRepository.GetByIdAsync(id, cancellationToken);
        if (execution is null)
        {
            throw new InvalidOperationException($"Execution {id} not found.");
        }

        execution.CompletedAt = completedAt;
        execution.Status = status;
        execution.DurationMinutes = (int?)((completedAt - execution.StartedAt).TotalMinutes);

        await _executionRepository.UpdateAsync(execution, cancellationToken);
        return execution;
    }

    public Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
        => _executionRepository.DeleteAsync(id, cancellationToken);

    public async Task<int> ImportFromInstanceAsync(Guid instanceId, DateTime since, CancellationToken cancellationToken = default)
    {
        var instance = await _instanceRepository.GetByIdAsync(instanceId, cancellationToken);
        if (instance is null)
        {
            throw new InvalidOperationException($"Instance {instanceId} not found.");
        }

        await using var client = new SqlServerInstanceClient(instance.ConnectionString);
        var entries = await client.GetJobHistoryAsync(since, cancellationToken);

        var count = 0;
        foreach (var entry in entries)
        {
            var process = await FindProcessByJobNameAsync(instanceId, entry.JobName, cancellationToken);

            var execution = new Execution
            {
                ProcessId = process?.Id,
                InstanceId = instanceId,
                Source = "Imported",
                StartedAt = entry.RunDateTime,
                CompletedAt = entry.RunDateTime.AddMinutes(entry.DurationMinutes),
                DurationMinutes = entry.DurationMinutes,
                Status = MapJobStatus(entry.Status),
                Notes = $"Imported from msdb job history. Step: {entry.StepName}. Message: {entry.Message}"
            };

            await _executionRepository.CreateAsync(execution, cancellationToken);
            count++;
        }

        return count;
    }

    public async Task CalibrateProcessDurationAsync(Guid processId, CancellationToken cancellationToken = default)
    {
        var process = await _processRepository.GetByIdAsync(processId, cancellationToken);
        if (process is null)
        {
            throw new InvalidOperationException($"Process {processId} not found.");
        }

        var executions = await _executionRepository.GetByProcessIdAsync(processId, cancellationToken);
        var completedExecutions = executions
            .Where(e => e.DurationMinutes.HasValue && e.Status == "Completed")
            .Select(e => e.DurationMinutes!.Value)
            .ToList();

        if (completedExecutions.Count == 0)
        {
            return;
        }

        var average = (int)Math.Round(completedExecutions.Average());
        process.EstimatedDurationMinutes = average;
        process.Touch();

        await _processRepository.UpdateAsync(process, cancellationToken);
    }

    private async Task<Process?> FindProcessByJobNameAsync(Guid instanceId, string jobName, CancellationToken cancellationToken)
    {
        var processes = await _processRepository.GetByInstanceIdAsync(instanceId, cancellationToken);
        return processes.FirstOrDefault(p =>
            p.Name.Equals(jobName, StringComparison.OrdinalIgnoreCase) ||
            (p.Description?.Equals(jobName, StringComparison.OrdinalIgnoreCase) ?? false));
    }

    private static string MapJobStatus(string status)
    {
        return status switch
        {
            "Succeeded" => "Completed",
            "Failed" => "Failed",
            "Canceled" => "Cancelled",
            "Retry" => "Running",
            _ => "Running"
        };
    }

    private static void ValidateExecution(Execution execution)
    {
        if (execution.InstanceId == Guid.Empty)
        {
            throw new ArgumentException("Instance is required.", nameof(execution));
        }

        if (execution.StartedAt == default)
        {
            throw new ArgumentException("StartedAt is required.", nameof(execution));
        }

        if (string.IsNullOrWhiteSpace(execution.Status))
        {
            execution.Status = "Running";
        }
    }
}
