namespace DbTraffic.Core.Rules;

public interface IRule
{
    string Name { get; }
    Task<RuleResult> EvaluateAsync(RuleContext context, CancellationToken cancellationToken = default);
}
