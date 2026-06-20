namespace DbTraffic.Shared.Models.Dmv;

public sealed class JobHistoryEntry
{
    public Guid JobId { get; set; }
    public string JobName { get; set; } = string.Empty;
    public int StepId { get; set; }
    public string StepName { get; set; } = string.Empty;
    public DateTime RunDateTime { get; set; }
    public int DurationMinutes { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? Message { get; set; }
}
