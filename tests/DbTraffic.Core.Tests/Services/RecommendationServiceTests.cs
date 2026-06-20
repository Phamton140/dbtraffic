using DbTraffic.Core.Entities;
using DbTraffic.Core.Enums;
using DbTraffic.Core.Repositories;
using DbTraffic.Core.Rules;
using DbTraffic.Core.Services;
using Moq;

namespace DbTraffic.Core.Tests.Services;

public class RecommendationServiceTests
{
    private readonly Mock<IProcessRepository> _processRepositoryMock = new();
    private readonly Mock<IInstanceRepository> _instanceRepositoryMock = new();
    private readonly Mock<IRiskCalculationService> _riskCalculationMock = new();
    private readonly RecommendationService _service;

    public RecommendationServiceTests()
    {
        _service = new RecommendationService(
            _processRepositoryMock.Object,
            _instanceRepositoryMock.Object,
            _riskCalculationMock.Object);
    }

    [Fact]
    public async Task FindWindowsAsync_When_Process_Not_Found_Returns_Empty()
    {
        _processRepositoryMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Process?)null);

        var result = await _service.FindWindowsAsync(new RecommendationRequest
        {
            ProcessId = Guid.NewGuid(),
            SearchStart = DateTime.UtcNow,
            SearchEnd = DateTime.UtcNow.AddHours(1)
        });

        Assert.Empty(result);
    }

    [Fact]
    public async Task FindWindowsAsync_Returns_Low_Risk_Windows_First()
    {
        var instanceId = Guid.NewGuid();
        var processId = Guid.NewGuid();
        var process = CreateProcess(processId, instanceId, 60);

        _processRepositoryMock.Setup(r => r.GetByIdAsync(processId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(process);
        _instanceRepositoryMock.Setup(r => r.GetByIdAsync(instanceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Instance { Id = instanceId, Name = "Test", ConnectionString = "Server=." });
        _processRepositoryMock.Setup(r => r.GetByInstanceIdAsync(instanceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Process>());

        _riskCalculationMock.Setup(r => r.CalculateAsync(It.IsAny<RuleContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((RuleContext ctx, CancellationToken _) => new RiskAssessment
            {
                TotalScore = ctx.ProposedStartTime.Hour == 2 ? 0 : 50,
                OverallLevel = ctx.ProposedStartTime.Hour == 2 ? RiskLevel.None : RiskLevel.High,
                Findings = new List<RuleResult>()
            });

        var searchStart = DateTime.UtcNow.Date.AddHours(1);
        var result = await _service.FindWindowsAsync(new RecommendationRequest
        {
            ProcessId = processId,
            SearchStart = searchStart,
            SearchEnd = searchStart.AddHours(4),
            GranularityMinutes = 60,
            MaxRecommendations = 5
        });

        Assert.Single(result);
        Assert.Equal(2, result[0].StartTime.Hour);
    }

    [Fact]
    public async Task FindWindowsAsync_Excludes_High_Risk_Windows()
    {
        var instanceId = Guid.NewGuid();
        var processId = Guid.NewGuid();
        var process = CreateProcess(processId, instanceId, 60);

        _processRepositoryMock.Setup(r => r.GetByIdAsync(processId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(process);
        _instanceRepositoryMock.Setup(r => r.GetByIdAsync(instanceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Instance { Id = instanceId, Name = "Test", ConnectionString = "Server=." });
        _processRepositoryMock.Setup(r => r.GetByInstanceIdAsync(instanceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Process>());

        _riskCalculationMock.Setup(r => r.CalculateAsync(It.IsAny<RuleContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RiskAssessment
            {
                TotalScore = 75,
                OverallLevel = RiskLevel.High,
                Findings = new List<RuleResult>()
            });

        var result = await _service.FindWindowsAsync(new RecommendationRequest
        {
            ProcessId = processId,
            SearchStart = DateTime.UtcNow,
            SearchEnd = DateTime.UtcNow.AddHours(4),
            GranularityMinutes = 60
        });

        Assert.Empty(result);
    }

    private static Process CreateProcess(Guid id, Guid instanceId, int durationMinutes)
    {
        return new Process
        {
            Id = id,
            InstanceId = instanceId,
            Name = "Test Process",
            ProcessType = ProcessType.SqlAgentJob,
            EstimatedDurationMinutes = durationMinutes,
            Objects = new List<ProcessObject>(),
            Schedules = new List<ProcessSchedule>()
        };
    }
}
