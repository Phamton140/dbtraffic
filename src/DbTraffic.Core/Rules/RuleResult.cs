namespace DbTraffic.Core.Rules;

public sealed class RuleResult
{
    public string RuleName { get; init; } = string.Empty;
    public RiskLevel Level { get; init; }
    public double Score { get; init; }
    public string Message { get; init; } = string.Empty;
    public Dictionary<string, object> Details { get; init; } = new();

    public static RuleResult Ok(string ruleName) =>
        new()
        {
            RuleName = ruleName,
            Level = RiskLevel.None,
            Score = 0,
            Message = "No se detectaron problemas."
        };

    public static RuleResult Warning(string ruleName, double score, string message, Dictionary<string, object>? details = null) =>
        new()
        {
            RuleName = ruleName,
            Level = RiskLevel.Medium,
            Score = score,
            Message = message,
            Details = details ?? new Dictionary<string, object>()
        };

    public static RuleResult High(string ruleName, double score, string message, Dictionary<string, object>? details = null) =>
        new()
        {
            RuleName = ruleName,
            Level = RiskLevel.High,
            Score = score,
            Message = message,
            Details = details ?? new Dictionary<string, object>()
        };

    public static RuleResult Critical(string ruleName, double score, string message, Dictionary<string, object>? details = null) =>
        new()
        {
            RuleName = ruleName,
            Level = RiskLevel.Critical,
            Score = score,
            Message = message,
            Details = details ?? new Dictionary<string, object>()
        };
}
