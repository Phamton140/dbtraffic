using DbTraffic.Core.Rules;

namespace DbTraffic.Core.Services;

public interface IRecommendationService
{
    Task<IReadOnlyList<RecommendationWindow>> FindWindowsAsync(RecommendationRequest request, CancellationToken cancellationToken = default);
}

public sealed class RecommendationRequest
{
    public Guid ProcessId { get; set; }
    public DateTime SearchStart { get; set; }
    public DateTime SearchEnd { get; set; }
    public int GranularityMinutes { get; set; } = 30;
    public int MaxRecommendations { get; set; } = 5;
}

public sealed class RecommendationWindow
{
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public double RiskScore { get; set; }
    public RiskLevel RiskLevel { get; set; }
}
