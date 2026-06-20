using DbTraffic.Core.Rules;

namespace DbTraffic.Core.Services;

public interface IRiskCalculationService
{
    Task<RiskAssessment> CalculateAsync(RuleContext context, CancellationToken cancellationToken = default);
}

public sealed class RiskAssessment
{
    public double TotalScore { get; init; }
    public RiskLevel OverallLevel { get; init; }
    public IReadOnlyList<RuleResult> Findings { get; init; } = new List<RuleResult>();
}
