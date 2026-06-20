using DbTraffic.Core.Entities;
using DbTraffic.Core.Enums;
using DbTraffic.Core.Rules;

namespace DbTraffic.Core.Tests.Rules;

public class HighIntensityOverlapRuleTests
{
    private readonly HighIntensityOverlapRule _rule = new();

    [Fact]
    public async Task EvaluateAsync_When_No_High_Intensity_Overlap_Returns_Ok()
    {
        var context = CreateContext(
            cpuIntensity: IntensityLevel.Low,
            overlappingProcesses: new List<Process>());

        var result = await _rule.EvaluateAsync(context);

        Assert.Equal(RiskLevel.None, result.Level);
    }

    [Fact]
    public async Task EvaluateAsync_When_High_Intensity_Overlap_Returns_Warning_Or_Higher()
    {
        var overlapping = new List<Process>
        {
            new()
            {
                Id = Guid.NewGuid(),
                Name = "Heavy IO",
                InstanceId = Guid.NewGuid(),
                IoIntensity = IntensityLevel.Critical
            }
        };

        var context = CreateContext(
            cpuIntensity: IntensityLevel.High,
            overlappingProcesses: overlapping);

        var result = await _rule.EvaluateAsync(context);

        Assert.True(result.Level >= RiskLevel.Medium);
        Assert.True(result.Score > 0);
    }

    private static RuleContext CreateContext(
        IntensityLevel cpuIntensity,
        List<Process>? overlappingProcesses = null)
    {
        var processId = Guid.NewGuid();
        return new RuleContext
        {
            Process = new Process
            {
                Id = processId,
                Name = "Test",
                InstanceId = Guid.NewGuid(),
                CpuIntensity = cpuIntensity,
                IoIntensity = IntensityLevel.Low,
                MemoryIntensity = IntensityLevel.Low
            },
            Instance = new Instance { Id = Guid.NewGuid(), Name = "Test Instance", ConnectionString = "Server=." },
            ProposedStartTime = DateTime.UtcNow,
            OverlappingProcesses = overlappingProcesses ?? new List<Process>(),
            ProcessObjects = new List<ProcessObject>(),
            ObjectsByProcessId = new Dictionary<Guid, IReadOnlyList<ProcessObject>>
            {
                [processId] = new List<ProcessObject>()
            }
        };
    }
}
