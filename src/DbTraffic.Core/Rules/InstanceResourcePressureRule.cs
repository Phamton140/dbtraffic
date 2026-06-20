namespace DbTraffic.Core.Rules;

public sealed class InstanceResourcePressureRule : IRule
{
    public string Name => "Instance Resource Pressure";

    public Task<RuleResult> EvaluateAsync(RuleContext context, CancellationToken cancellationToken = default)
    {
        if (context.ResourceState is null)
        {
            return Task.FromResult(RuleResult.Ok(Name));
        }

        var score = 0.0;
        var details = new Dictionary<string, object>();

        if (context.ResourceState.CpuPercent > 80)
        {
            score += 30;
            details["CpuPressure"] = context.ResourceState.CpuPercent;
        }

        if (context.ResourceState.MemoryPercent > 85)
        {
            score += 30;
            details["MemoryPressure"] = context.ResourceState.MemoryPercent;
        }

        if (context.ResourceState.BlockingSessions > 5)
        {
            score += 40;
            details["BlockingSessions"] = context.ResourceState.BlockingSessions;
        }

        if (score == 0)
        {
            return Task.FromResult(RuleResult.Ok(Name));
        }

        return Task.FromResult(new RuleResult
        {
            RuleName = Name,
            Level = score >= 60 ? RiskLevel.High : RiskLevel.Medium,
            Score = score,
            Message = "La instancia objetivo presenta presión de recursos.",
            Details = details
        });
    }
}
