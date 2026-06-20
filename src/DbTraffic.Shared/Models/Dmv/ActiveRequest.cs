namespace DbTraffic.Shared.Models.Dmv;

public sealed class ActiveRequest
{
    public int SessionId { get; set; }
    public int? RequestId { get; set; }
    public string? Status { get; set; }
    public string? Command { get; set; }
    public string? SqlText { get; set; }
    public DateTime? StartTime { get; set; }
    public string? DatabaseName { get; set; }
    public string? LoginName { get; set; }
    public string? ProgramName { get; set; }
}
