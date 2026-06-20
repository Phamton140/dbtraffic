using DbTraffic.Core.Entities;
using DbTraffic.Core.Rules;
using DbTraffic.Core.Services;

namespace DbTraffic.Core.Tests.Rules;

public class RiskCalculationServiceTests
{
    [Fact]
    public async Task CalculateAsync_With_No_Rules_Returns_None()
    {
        var service = new RiskCalculationService(Array.Empty<IRule>());
        var context = CreateContext();

        var assessment = await service.CalculateAsync(context);

        Assert.Equal(RiskLevel.None, assessment.OverallLevel);
        Assert.Equal(0, assessment.TotalScore);
        Assert.Empty(assessment.Findings);
    }

    [Fact]
    public async Task CalculateAsync_Sums_Scores_And_Determines_Level()
    {
        var rules = new List<IRule>
        {
            new FixedScoreRule("Rule A", RiskLevel.High, 40),
            new FixedScoreRule("Rule B", RiskLevel.Medium, 20)
        };

        var service = new RiskCalculationService(rules);
        var context = CreateContext();

        var assessment = await service.CalculateAsync(context);

        Assert.Equal(60, assessment.TotalScore);
        Assert.Equal(RiskLevel.High, assessment.OverallLevel);
        Assert.Equal(2, assessment.Findings.Count);
    }

    [Fact]
    public async Task CalculateAsync_Critical_Rule_Drives_High_Level()
    {
        var rules = new List<IRule>
        {
            new FixedScoreRule("Rule A", RiskLevel.Critical, 80)
        };

        var service = new RiskCalculationService(rules);
        var context = CreateContext();

        var assessment = await service.CalculateAsync(context);

        Assert.Equal(RiskLevel.Critical, assessment.OverallLevel);
    }

    private static RuleContext CreateContext()
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
            }
        };
    }

    private sealed class FixedScoreRule : IRule
    {
        private readonly RiskLevel _level;
        private readonly double _score;

        public FixedScoreRule(string name, RiskLevel level, double score)
        {
            Name = name;
            _level = level;
            _score = score;
        }

        public string Name { get; }

        public Task<RuleResult> EvaluateAsync(RuleContext context, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new RuleResult
            {
                RuleName = Name,
                Level = _level,
                Score = _score,
                Message = "Fixed score"
            });
        }
    }
}
