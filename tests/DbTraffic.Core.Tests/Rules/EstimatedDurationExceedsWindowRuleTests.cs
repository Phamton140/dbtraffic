using DbTraffic.Core.Entities;
using DbTraffic.Core.Rules;

namespace DbTraffic.Core.Tests.Rules;

public class EstimatedDurationExceedsWindowRuleTests
{
    private readonly EstimatedDurationExceedsWindowRule _rule = new();

    [Fact]
    public async Task EvaluateAsync_When_Duration_Fits_Window_Returns_Ok()
    {
        var context = CreateContext(
            estimatedDuration: 60,
            windowStart: TimeSpan.FromHours(2),
            windowEnd: TimeSpan.FromHours(4));

        var result = await _rule.EvaluateAsync(context);

        Assert.Equal(RiskLevel.None, result.Level);
    }

    [Fact]
    public async Task EvaluateAsync_When_Duration_Exceeds_Window_Returns_Risk()
    {
        var context = CreateContext(
            estimatedDuration: 180,
            windowStart: TimeSpan.FromHours(2),
            windowEnd: TimeSpan.FromHours(3));

        var result = await _rule.EvaluateAsync(context);

        Assert.True(result.Level >= RiskLevel.Medium);
        Assert.True(result.Score > 0);
    }

    [Fact]
    public async Task EvaluateAsync_When_No_Window_Returns_Ok()
    {
        var context = CreateContext(
            estimatedDuration: 180,
            windowStart: null,
            windowEnd: null);

        var result = await _rule.EvaluateAsync(context);

        Assert.Equal(RiskLevel.None, result.Level);
    }

    private static RuleContext CreateContext(
        int estimatedDuration,
        TimeSpan? windowStart,
        TimeSpan? windowEnd)
    {
        var processId = Guid.NewGuid();
        return new RuleContext
        {
            Process = new Process
            {
                Id = processId,
                Name = "Test",
                InstanceId = Guid.NewGuid(),
                EstimatedDurationMinutes = estimatedDuration,
                PreferredWindowStart = windowStart,
                PreferredWindowEnd = windowEnd
            },
            Instance = new Instance { Id = Guid.NewGuid(), Name = "Test Instance", ConnectionString = "Server=." },
            ProposedStartTime = DateTime.UtcNow,
            OverlappingProcesses = new List<Process>(),
            ProcessObjects = new List<ProcessObject>(),
            ObjectsByProcessId = new Dictionary<Guid, IReadOnlyList<ProcessObject>>
            {
                [processId] = new List<ProcessObject>()
            }
        };
    }
}
