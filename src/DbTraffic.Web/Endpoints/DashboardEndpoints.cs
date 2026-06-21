using DbTraffic.Core.Entities;
using DbTraffic.Core.Repositories;
using DbTraffic.Shared.Models;

namespace DbTraffic.Web.Endpoints;

public static class DashboardEndpoints
{
    public static IEndpointRouteBuilder MapDashboardEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/dashboard/summary", async (
            IInstanceRepository instances,
            IProcessRepository processes,
            IExecutionRepository executions,
            CancellationToken cancellationToken) =>
        {
            var instanceList = await instances.GetAllAsync(cancellationToken);
            var processList = await processes.GetAllAsync(cancellationToken);
            var executionList = await executions.GetAllAsync(cancellationToken);

            var totalExecutions = executionList.Count;
            var successCount = executionList.Count(e => e.Status == "Completed");
            var failureCount = executionList.Count(e => e.Status == "Failed");

            var topProcess = executionList
                .Where(e => e.ProcessId.HasValue)
                .GroupBy(e => e.ProcessId!.Value)
                .Select(g => new { ProcessId = g.Key, Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .FirstOrDefault();

            var topProcessName = topProcess is not null
                ? processList.FirstOrDefault(p => p.Id == topProcess.ProcessId)?.Name ?? "-"
                : "-";

            var latestExecutions = executionList
                .OrderByDescending(e => e.StartedAt)
                .Take(5)
                .Select(e => new DashboardExecution
                {
                    Id = e.Id,
                    ProcessName = e.ProcessId.HasValue
                        ? processList.FirstOrDefault(p => p.Id == e.ProcessId.Value)?.Name ?? "-"
                        : "-",
                    InstanceName = instanceList.FirstOrDefault(i => i.Id == e.InstanceId)?.Name ?? "-",
                    StartedAt = e.StartedAt,
                    Status = e.Status,
                    DurationMinutes = e.DurationMinutes
                })
                .ToList();

            var summary = new DashboardSummary
            {
                TotalInstances = instanceList.Count,
                TotalProcesses = processList.Count,
                TotalExecutions = totalExecutions,
                SuccessRate = totalExecutions > 0 ? successCount * 100.0 / totalExecutions : 0,
                FailureRate = totalExecutions > 0 ? failureCount * 100.0 / totalExecutions : 0,
                LastExecutionAt = executionList.Max(e => (DateTime?)e.StartedAt),
                TopProcessByExecutions = topProcessName,
                TopProcessExecutionCount = topProcess?.Count ?? 0,
                LatestExecutions = latestExecutions
            };

            return Results.Ok(summary);
        });

        return app;
    }
}
