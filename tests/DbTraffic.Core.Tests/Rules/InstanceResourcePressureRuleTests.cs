using DbTraffic.Core.Entities;
using DbTraffic.Core.Rules;

namespace DbTraffic.Core.Tests.Rules;

public class InstanceResourcePressureRuleTests
{
    private readonly InstanceResourcePressureRule _rule = new();

    [Fact]
    public async Task EvaluateAsync_When_No_Resource_State_Returns_Ok()
    {
        var context = CreateContext(resourceState: null);

        var result = await _rule.EvaluateAsync(context);

        Assert.Equal(RiskLevel.None, result.Level);
    }

    [Fact]
    public async Task EvaluateAsync_When_High_Cpu_Returns_Risk()
    {
        var context = CreateContext(resourceState: new InstanceResourceState
        {
            CpuPercent = 90,
            MemoryPercent = 40,
            BlockingSessions = 0
        });

        var result = await _rule.EvaluateAsync(context);

        Assert.True(result.Level >= RiskLevel.Medium);
        Assert.True(result.Score > 0);
    }

    [Fact]
    public async Task EvaluateAsync_When_Low_Resources_Returns_Ok()
    {
        var context = CreateContext(resourceState: new InstanceResourceState
        {
            CpuPercent = 30,
            MemoryPercent = 40,
            BlockingSessions = 0
        });

        var result = await _rule.EvaluateAsync(context);

        Assert.Equal(RiskLevel.None, result.Level);
    }

    private static RuleContext CreateContext(InstanceResourceState? resourceState)
    {
        var processId = Guid.NewGuid();
        return new RuleContext
        {
            Process = new Process { Id = processId, Name = "Test", InstanceId = Guid.NewGuid() },
            Instance = new Instance { Id = Guid.NewGuid(), Name = "Test Instance", ConnectionString = "Server=." },
            ProposedStartTime = DateTime.UtcNow,
            OverlappingProcesses = new List<Process>(),
            ProcessObjects = new List<ProcessObject>(),
            ObjectsByProcessId = new Dictionary<Guid, IReadOnlyList<ProcessObject>>
            {
                [processId] = new List<ProcessObject>()
            },
            ResourceState = resourceState
        };
    }
}
