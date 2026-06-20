using DbTraffic.Core.Rules;

namespace DbTraffic.Core.Services;

public sealed class RiskCalculationService : IRiskCalculationService
{
    private readonly IEnumerable<IRule> _rules;

    public RiskCalculationService(IEnumerable<IRule> rules)
    {
        _rules = rules;
    }

    public async Task<RiskAssessment> CalculateAsync(RuleContext context, CancellationToken cancellationToken = default)
    {
        var findings = new List<RuleResult>();

        foreach (var rule in _rules)
        {
            var result = await rule.EvaluateAsync(context, cancellationToken);
            findings.Add(result);
        }

        var totalScore = findings.Sum(f => f.Score);
        var maxLevel = findings.Any() ? findings.Max(f => f.Level) : RiskLevel.None;

        var overallLevel = totalScore switch
        {
            >= 75 => RiskLevel.Critical,
            >= 50 => RiskLevel.High,
            >= 25 => RiskLevel.Medium,
            > 0 => RiskLevel.Low,
            _ => maxLevel
        };

        return new RiskAssessment
        {
            TotalScore = Math.Min(100, totalScore),
            OverallLevel = overallLevel,
            Findings = findings
        };
    }
}
