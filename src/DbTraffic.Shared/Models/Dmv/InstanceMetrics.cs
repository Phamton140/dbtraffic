namespace DbTraffic.Shared.Models.Dmv;

public sealed class InstanceMetrics
{
    public int ActiveRequests { get; set; }
    public int BlockingSessions { get; set; }
    public long WaitTimeMs { get; set; }
    public double CpuPercent { get; set; }
    public double MemoryPercent { get; set; }
    public IReadOnlyList<ActiveRequest> ActiveRequestsDetail { get; set; } = new List<ActiveRequest>();
}
