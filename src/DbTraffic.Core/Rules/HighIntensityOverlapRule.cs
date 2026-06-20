using DbTraffic.Core.Entities;
using DbTraffic.Core.Enums;

namespace DbTraffic.Core.Rules;

public sealed class HighIntensityOverlapRule : IRule
{
    public string Name => "High Intensity Overlap";

    public Task<RuleResult> EvaluateAsync(RuleContext context, CancellationToken cancellationToken = default)
    {
        var currentIntensity = (int)context.Process.CpuIntensity + (int)context.Process.IoIntensity + (int)context.Process.MemoryIntensity;

        var highIntensityOverlaps = context.OverlappingProcesses
            .Where(p => IsHighIntensity(p))
            .Select(p => p.Name)
            .ToList();

        if (highIntensityOverlaps.Count == 0)
        {
            return Task.FromResult(RuleResult.Ok(Name));
        }

        var score = Math.Min(100, highIntensityOverlaps.Count * 20 + currentIntensity * 5);
        var level = score >= 70 ? RiskLevel.High : RiskLevel.Medium;

        return Task.FromResult(new RuleResult
        {
            RuleName = Name,
            Level = level,
            Score = score,
            Message = $"{highIntensityOverlaps.Count} proceso(s) de alta intensidad se solapan con la ejecución propuesta.",
            Details = new Dictionary<string, object>
            {
                ["OverlappingProcesses"] = highIntensityOverlaps
            }
        });
    }

    private static bool IsHighIntensity(Process process)
    {
        return process.CpuIntensity >= IntensityLevel.High ||
               process.IoIntensity >= IntensityLevel.High ||
               process.MemoryIntensity >= IntensityLevel.High;
    }
}
