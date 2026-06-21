using DbTraffic.Core.Entities;
using DbTraffic.Core.Repositories;
using DbTraffic.Infrastructure.Discovery;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace DbTraffic.Infrastructure.Tests;

public class DiscoveryServiceUnitTests
{
    private readonly Mock<IInstanceRepository> _instanceRepoMock = new();
    private readonly Mock<IDiscoveryRepository> _discoveryRepoMock = new();
    private readonly DiscoveryService _service;

    public DiscoveryServiceUnitTests()
    {
        _service = new DiscoveryService(
            _instanceRepoMock.Object,
            _discoveryRepoMock.Object,
            NullLogger<DiscoveryService>.Instance);
    }

    [Fact]
    public async Task DiscoverInstanceAsync_Guid_When_Instance_Not_Found_Should_Return_Early()
    {
        _instanceRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Instance?)null);

        await _service.DiscoverInstanceAsync(Guid.NewGuid());

        _instanceRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Once);
        _discoveryRepoMock.Verify(r => r.SaveJobsAsync(It.IsAny<Guid>(), It.IsAny<IEnumerable<DiscoveredJob>>(), It.IsAny<CancellationToken>()), Times.Never);
        _discoveryRepoMock.Verify(r => r.SaveObjectsAsync(It.IsAny<Guid>(), It.IsAny<IEnumerable<DiscoveredObject>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DiscoverInstanceAsync_Guid_When_Instance_Inactive_Should_Skip_Discovery()
    {
        var instanceId = Guid.NewGuid();
        _instanceRepoMock.Setup(r => r.GetByIdAsync(instanceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Instance { Id = instanceId, IsActive = false });

        await _service.DiscoverInstanceAsync(instanceId);

        _discoveryRepoMock.Verify(r => r.SaveJobsAsync(It.IsAny<Guid>(), It.IsAny<IEnumerable<DiscoveredJob>>(), It.IsAny<CancellationToken>()), Times.Never);
        _discoveryRepoMock.Verify(r => r.SaveObjectsAsync(It.IsAny<Guid>(), It.IsAny<IEnumerable<DiscoveredObject>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DiscoverInstanceAsync_Guid_When_Instance_NotFound_CancellationToken_Respected()
    {
        using var cts = new CancellationTokenSource();
        _instanceRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Instance?)null);

        await _service.DiscoverInstanceAsync(Guid.NewGuid(), cts.Token);

        _instanceRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<Guid>(), cts.Token), Times.Once);
    }

    [Fact]
    public async Task DiscoverAllActiveInstancesAsync_Should_Not_Throw_When_Connection_Fails()
    {
        _instanceRepoMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Instance>
            {
                new() { Id = Guid.NewGuid(), Name = "Faulty Instance", IsActive = true, ConnectionString = "Server=invalid;Connect Timeout=1" }
            });

        var exception = await Record.ExceptionAsync(() => _service.DiscoverAllActiveInstancesAsync());

        Assert.Null(exception);
        _instanceRepoMock.Verify(r => r.GetAllAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DiscoverAllActiveInstancesAsync_Should_Continue_After_Failure()
    {
        _instanceRepoMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Instance>
            {
                new() { Id = Guid.NewGuid(), Name = "Faulty Instance 1", IsActive = true, ConnectionString = "Server=invalid1;Connect Timeout=1" },
                new() { Id = Guid.NewGuid(), Name = "Faulty Instance 2", IsActive = true, ConnectionString = "Server=invalid2;Connect Timeout=1" }
            });

        var exception = await Record.ExceptionAsync(() => _service.DiscoverAllActiveInstancesAsync());

        Assert.Null(exception);
        _instanceRepoMock.Verify(r => r.GetAllAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DiscoverAllActiveInstancesAsync_When_No_Instances_Should_Not_Throw()
    {
        _instanceRepoMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Instance>());

        var exception = await Record.ExceptionAsync(() => _service.DiscoverAllActiveInstancesAsync());

        Assert.Null(exception);
    }
}
