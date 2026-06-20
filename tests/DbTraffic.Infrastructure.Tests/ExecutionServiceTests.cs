using DbTraffic.Core.Entities;
using DbTraffic.Core.Repositories;
using DbTraffic.Core.Services;
using DbTraffic.Infrastructure.Services;
using Moq;

namespace DbTraffic.Infrastructure.Tests;

public class ExecutionServiceTests
{
    private readonly Mock<IExecutionRepository> _executionRepositoryMock = new();
    private readonly Mock<IProcessRepository> _processRepositoryMock = new();
    private readonly Mock<IInstanceRepository> _instanceRepositoryMock = new();
    private readonly ExecutionService _service;

    public ExecutionServiceTests()
    {
        _service = new ExecutionService(
            _executionRepositoryMock.Object,
            _processRepositoryMock.Object,
            _instanceRepositoryMock.Object);
    }

    [Fact]
    public async Task CreateAsync_Sets_Source_To_Manual_And_Persists()
    {
        var instanceId = Guid.NewGuid();
        var execution = new Execution
        {
            InstanceId = instanceId,
            StartedAt = DateTime.UtcNow,
            Status = "Running"
        };

        _executionRepositoryMock.Setup(r => r.CreateAsync(It.IsAny<Execution>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Execution e, CancellationToken _) => e);

        var result = await _service.CreateAsync(execution);

        Assert.Equal("Manual", result.Source);
        _executionRepositoryMock.Verify(r => r.CreateAsync(execution, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CompleteAsync_When_Execution_Not_Found_Throws()
    {
        _executionRepositoryMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Execution?)null);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.CompleteAsync(Guid.NewGuid(), DateTime.UtcNow, "Completed"));
    }

    [Fact]
    public async Task CompleteAsync_Sets_Duration_And_Status()
    {
        var executionId = Guid.NewGuid();
        var startedAt = DateTime.UtcNow.AddMinutes(-10);
        var completedAt = DateTime.UtcNow;
        var execution = new Execution
        {
            Id = executionId,
            StartedAt = startedAt,
            Status = "Running"
        };

        _executionRepositoryMock.Setup(r => r.GetByIdAsync(executionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(execution);

        var result = await _service.CompleteAsync(executionId, completedAt, "Completed");

        Assert.Equal("Completed", result.Status);
        Assert.True(result.DurationMinutes.HasValue);
        Assert.Equal(10, result.DurationMinutes.Value);
    }

    [Fact]
    public async Task CalibrateProcessDurationAsync_Updates_Process_With_Average_Duration()
    {
        var processId = Guid.NewGuid();
        var process = new Process { Id = processId, EstimatedDurationMinutes = 30 };

        _processRepositoryMock.Setup(r => r.GetByIdAsync(processId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(process);
        _executionRepositoryMock.Setup(r => r.GetByProcessIdAsync(processId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Execution>
            {
                new() { ProcessId = processId, Status = "Completed", DurationMinutes = 10 },
                new() { ProcessId = processId, Status = "Completed", DurationMinutes = 20 },
                new() { ProcessId = processId, Status = "Failed", DurationMinutes = 100 }
            });

        await _service.CalibrateProcessDurationAsync(processId);

        Assert.Equal(15, process.EstimatedDurationMinutes ?? 0);
        _processRepositoryMock.Verify(r => r.UpdateAsync(process, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CalibrateProcessDurationAsync_When_No_Completed_Executions_Does_Not_Update()
    {
        var processId = Guid.NewGuid();
        var process = new Process { Id = processId, EstimatedDurationMinutes = 30 };

        _processRepositoryMock.Setup(r => r.GetByIdAsync(processId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(process);
        _executionRepositoryMock.Setup(r => r.GetByProcessIdAsync(processId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Execution>());

        await _service.CalibrateProcessDurationAsync(processId);

        Assert.Equal(30, process.EstimatedDurationMinutes);
        _processRepositoryMock.Verify(r => r.UpdateAsync(It.IsAny<Process>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
