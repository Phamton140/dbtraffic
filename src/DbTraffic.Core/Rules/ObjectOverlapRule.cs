namespace DbTraffic.Core.Rules;

public sealed class ObjectOverlapRule : IRule
{
    public string Name => "Object Overlap";

    public Task<RuleResult> EvaluateAsync(RuleContext context, CancellationToken cancellationToken = default)
    {
        var currentCriticalObjects = context.ProcessObjects
            .Where(o => o.IsCritical)
            .Select(o => new { o.SchemaName, o.ObjectName })
            .ToHashSet();

        if (currentCriticalObjects.Count == 0)
        {
            return Task.FromResult(RuleResult.Ok(Name));
        }

        var conflicts = new List<string>();
        foreach (var other in context.OverlappingProcesses)
        {
            if (!context.ObjectsByProcessId.TryGetValue(other.Id, out var otherObjects))
            {
                continue;
            }

            var overlappingObjects = otherObjects
                .Where(o => o.IsCritical)
                .Where(o => currentCriticalObjects.Any(c =>
                    c.SchemaName.Equals(o.SchemaName, StringComparison.OrdinalIgnoreCase) &&
                    c.ObjectName.Equals(o.ObjectName, StringComparison.OrdinalIgnoreCase)))
                .ToList();

            foreach (var obj in overlappingObjects)
            {
                conflicts.Add($"{other.Name} -> {obj.SchemaName}.{obj.ObjectName}");
            }
        }

        if (conflicts.Count == 0)
        {
            return Task.FromResult(RuleResult.Ok(Name));
        }

        var score = Math.Min(100, conflicts.Count * 25);
        var level = score >= 75 ? RiskLevel.Critical : RiskLevel.High;

        return Task.FromResult(new RuleResult
        {
            RuleName = Name,
            Level = level,
            Score = score,
            Message = $"Se detectaron {conflicts.Count} conflicto(s) de objetos críticos con procesos solapados.",
            Details = new Dictionary<string, object>
            {
                ["Conflicts"] = conflicts
            }
        });
    }
}
