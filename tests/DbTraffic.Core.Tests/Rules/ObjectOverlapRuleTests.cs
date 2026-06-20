using DbTraffic.Core.Entities;
using DbTraffic.Core.Enums;
using DbTraffic.Core.Rules;

namespace DbTraffic.Core.Tests.Rules;

public class ObjectOverlapRuleTests
{
    private readonly ObjectOverlapRule _rule = new();

    [Fact]
    public async Task EvaluateAsync_When_No_Critical_Objects_Returns_Ok()
    {
        var context = CreateContext(
            processObjects: new List<ProcessObject>(),
            overlappingProcesses: new List<Process>());

        var result = await _rule.EvaluateAsync(context);

        Assert.Equal(RiskLevel.None, result.Level);
        Assert.Equal(0, result.Score);
    }

    [Fact]
    public async Task EvaluateAsync_When_Overlapping_Process_Uses_Same_Critical_Object_Returns_High_Risk()
    {
        var processId = Guid.NewGuid();
        var otherProcessId = Guid.NewGuid();

        var processObjects = new List<ProcessObject>
        {
            new()
            {
                ProcessId = processId,
                SchemaName = "dbo",
                ObjectName = "Orders",
                ObjectType = ObjectType.Table,
                IsCritical = true
            }
        };

        var otherObjects = new List<ProcessObject>
        {
            new()
            {
                ProcessId = otherProcessId,
                SchemaName = "dbo",
                ObjectName = "Orders",
                ObjectType = ObjectType.Table,
                IsCritical = true
            }
        };

        var context = CreateContext(
            processId: processId,
            processObjects: processObjects,
            overlappingProcesses: new List<Process>
            {
                new() { Id = otherProcessId, Name = "Other Process", InstanceId = Guid.NewGuid() }
            },
            objectsByProcessId: new Dictionary<Guid, IReadOnlyList<ProcessObject>>
            {
                [processId] = processObjects,
                [otherProcessId] = otherObjects
            });

        var result = await _rule.EvaluateAsync(context);

        Assert.True(result.Level >= RiskLevel.High);
        Assert.True(result.Score > 0);
        var conflicts = Assert.IsAssignableFrom<IReadOnlyList<string>>(result.Details["Conflicts"]);
        Assert.Contains(conflicts, c => c.Contains("Orders"));
    }

    private static RuleContext CreateContext(
        Guid? processId = null,
        List<ProcessObject>? processObjects = null,
        List<Process>? overlappingProcesses = null,
        Dictionary<Guid, IReadOnlyList<ProcessObject>>? objectsByProcessId = null)
    {
        var id = processId ?? Guid.NewGuid();
        var objects = processObjects ?? new List<ProcessObject>();
        var byProcessId = objectsByProcessId ?? new Dictionary<Guid, IReadOnlyList<ProcessObject>>();

        if (!byProcessId.ContainsKey(id))
        {
            byProcessId[id] = objects;
        }

        return new RuleContext
        {
            Process = new Process { Id = id, Name = "Test", InstanceId = Guid.NewGuid() },
            Instance = new Instance { Id = Guid.NewGuid(), Name = "Test Instance", ConnectionString = "Server=." },
            ProposedStartTime = DateTime.UtcNow,
            OverlappingProcesses = overlappingProcesses ?? new List<Process>(),
            ProcessObjects = objects,
            ObjectsByProcessId = byProcessId
        };
    }
}
