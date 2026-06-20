using DbTraffic.Core.Entities;
using DbTraffic.Core.Repositories;
using DbTraffic.Core.Services;
using DbTraffic.Infrastructure.Services;
using DbTraffic.Infrastructure.SqlServer;
using DbTraffic.Shared.Models;
using DbTraffic.Shared.Models.Dmv;
using Moq;

namespace DbTraffic.Infrastructure.Tests;

public class RiskContextProviderTests
{
    private readonly Mock<IProcessRepository> _processRepositoryMock = new();
    private readonly Mock<IInstanceRepository> _instanceRepositoryMock = new();
    private readonly Mock<ISqlServerInstanceClient> _instanceClientMock = new();
    private readonly RiskContextProvider _provider;

    public RiskContextProviderTests()
    {
        _provider = new RiskContextProvider(
            _processRepositoryMock.Object,
            _instanceRepositoryMock.Object,
            _instanceClientMock.Object);
    }

    [Fact]
    public async Task BuildContextAsync_Returns_Null_When_Process_Not_Found()
    {
        _processRepositoryMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Process?)null);

        var result = await _provider.BuildContextAsync(Guid.NewGuid(), DateTime.UtcNow);

        Assert.Null(result);
    }

    [Fact]
    public async Task BuildContextAsync_Populates_ResourceState_From_Instance_Metrics()
    {
        var instanceId = Guid.NewGuid();
        var processId = Guid.NewGuid();
        var process = new Process
        {
            Id = processId,
            InstanceId = instanceId,
            Name = "Test Process",
            EstimatedDurationMinutes = 60,
            Objects = new List<ProcessObject>(),
            Schedules = new List<ProcessSchedule>()
        };

        _processRepositoryMock.Setup(r => r.GetByIdAsync(processId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(process);
        _instanceRepositoryMock.Setup(r => r.GetByIdAsync(instanceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Instance { Id = instanceId, Name = "Test", ConnectionString = "Server=." });
        _processRepositoryMock.Setup(r => r.GetByInstanceIdAsync(instanceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Process>());

        _instanceClientMock.Setup(c => c.GetInstanceMetricsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new InstanceMetrics
            {
                ActiveRequests = 5,
                BlockingSessions = 3,
                CpuPercent = 12.5,
                MemoryPercent = 67.0
            });

        var result = await _provider.BuildContextAsync(processId, DateTime.UtcNow);

        Assert.NotNull(result);
        Assert.NotNull(result.ResourceState);
        Assert.Equal(5, result.ResourceState.ActiveRequests);
        Assert.Equal(3, result.ResourceState.BlockingSessions);
        Assert.Equal(12.5, result.ResourceState.CpuPercent);
        Assert.Equal(67.0, result.ResourceState.MemoryPercent);
    }

    [Fact]
    public async Task BuildContextAsync_Returns_Null_ResourceState_When_Metrics_Fail()
    {
        var instanceId = Guid.NewGuid();
        var processId = Guid.NewGuid();
        var process = new Process
        {
            Id = processId,
            InstanceId = instanceId,
            Name = "Test Process",
            EstimatedDurationMinutes = 60,
            Objects = new List<ProcessObject>(),
            Schedules = new List<ProcessSchedule>()
        };

        _processRepositoryMock.Setup(r => r.GetByIdAsync(processId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(process);
        _instanceRepositoryMock.Setup(r => r.GetByIdAsync(instanceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Instance { Id = instanceId, Name = "Test", ConnectionString = "Server=." });
        _processRepositoryMock.Setup(r => r.GetByInstanceIdAsync(instanceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Process>());

        _instanceClientMock.Setup(c => c.GetInstanceMetricsAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Connection failed"));

        var result = await _provider.BuildContextAsync(processId, DateTime.UtcNow);

        Assert.NotNull(result);
        Assert.Null(result.ResourceState);
    }
}
