namespace DbTraffic.Core.Rules;

public sealed class EstimatedDurationExceedsWindowRule : IRule
{
    public string Name => "Estimated Duration Exceeds Window";

    public Task<RuleResult> EvaluateAsync(RuleContext context, CancellationToken cancellationToken = default)
    {
        if (!context.Process.PreferredWindowStart.HasValue || !context.Process.PreferredWindowEnd.HasValue)
        {
            return Task.FromResult(RuleResult.Ok(Name));
        }

        var duration = context.Process.EstimatedDurationMinutes ?? 60;
        var windowMinutes = (context.Process.PreferredWindowEnd.Value - context.Process.PreferredWindowStart.Value).TotalMinutes;

        if (windowMinutes <= 0)
        {
            return Task.FromResult(RuleResult.Ok(Name));
        }

        if (duration <= windowMinutes)
        {
            return Task.FromResult(RuleResult.Ok(Name));
        }

        var ratio = duration / windowMinutes;
        var score = Math.Min(100, (ratio - 1) * 50);

        return Task.FromResult(new RuleResult
        {
            RuleName = Name,
            Level = score >= 50 ? RiskLevel.High : RiskLevel.Medium,
            Score = score,
            Message = $"La duración estimada ({duration} min) excede la ventana preferida ({windowMinutes:F0} min).",
            Details = new Dictionary<string, object>
            {
                ["EstimatedDurationMinutes"] = duration,
                ["WindowMinutes"] = windowMinutes,
                ["Ratio"] = ratio
            }
        });
    }
}
