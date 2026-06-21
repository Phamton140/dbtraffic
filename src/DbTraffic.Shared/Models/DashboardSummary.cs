namespace DbTraffic.Shared.Models;

public sealed class DashboardSummary
{
    public int TotalInstances { get; set; }
    public int TotalProcesses { get; set; }
    public int TotalExecutions { get; set; }
    public double SuccessRate { get; set; }
    public double FailureRate { get; set; }
    public DateTime? LastExecutionAt { get; set; }
    public string TopProcessByExecutions { get; set; } = "-";
    public int TopProcessExecutionCount { get; set; }
    public List<DashboardExecution> LatestExecutions { get; set; } = new();
}

public sealed class DashboardExecution
{
    public Guid Id { get; set; }
    public string ProcessName { get; set; } = "-";
    public string InstanceName { get; set; } = "-";
    public DateTime StartedAt { get; set; }
    public string Status { get; set; } = "Unknown";
    public int? DurationMinutes { get; set; }
}
