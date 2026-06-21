using DbTraffic.Core.Entities;
using DbTraffic.Core.Repositories;
using DbTraffic.Infrastructure.Discovery;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;

namespace DbTraffic.Infrastructure.Tests;

public class DiscoveryWorkerTests
{
    [Fact]
    public async Task ExecuteAsync_Should_Call_DiscoverAllActiveInstancesAsync()
    {
        var instanceRepoMock = new Mock<IInstanceRepository>();
        instanceRepoMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Instance>());

        var discoveryService = new DiscoveryService(
            instanceRepoMock.Object,
            new Mock<IDiscoveryRepository>().Object,
            NullLogger<DiscoveryService>.Instance);

        var rootProvider = CreateWorkerEnvironment(discoveryService);

        var worker = new DiscoveryWorker(
            rootProvider.Object,
            NullLogger<DiscoveryWorker>.Instance,
            Options.Create(new DiscoveryWorkerOptions { Interval = TimeSpan.FromMilliseconds(20) }));

        await worker.StartAsync(CancellationToken.None);
        await Task.Delay(300);
        await worker.StopAsync(CancellationToken.None);

        instanceRepoMock.Verify(r => r.GetAllAsync(It.IsAny<CancellationToken>()), Times.AtLeastOnce());
    }

    [Fact]
    public async Task ExecuteAsync_Should_Handle_Exceptions_Gracefully()
    {
        var instanceRepoMock = new Mock<IInstanceRepository>();
        instanceRepoMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Test exception"));

        var discoveryService = new DiscoveryService(
            instanceRepoMock.Object,
            new Mock<IDiscoveryRepository>().Object,
            NullLogger<DiscoveryService>.Instance);

        var rootProvider = CreateWorkerEnvironment(discoveryService);

        var worker = new DiscoveryWorker(
            rootProvider.Object,
            NullLogger<DiscoveryWorker>.Instance,
            Options.Create(new DiscoveryWorkerOptions { Interval = TimeSpan.FromMilliseconds(20) }));

        var exception = await Record.ExceptionAsync(async () =>
        {
            await worker.StartAsync(CancellationToken.None);
            await Task.Delay(300);
            await worker.StopAsync(CancellationToken.None);
        });

        Assert.Null(exception);
        instanceRepoMock.Verify(r => r.GetAllAsync(It.IsAny<CancellationToken>()), Times.AtLeastOnce());
    }

    [Fact]
    public async Task ExecuteAsync_Should_Stop_Gracefully_On_Cancellation()
    {
        var instanceRepoMock = new Mock<IInstanceRepository>();
        instanceRepoMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Instance>());

        var discoveryService = new DiscoveryService(
            instanceRepoMock.Object,
            new Mock<IDiscoveryRepository>().Object,
            NullLogger<DiscoveryService>.Instance);

        var rootProvider = CreateWorkerEnvironment(discoveryService);

        var worker = new DiscoveryWorker(
            rootProvider.Object,
            NullLogger<DiscoveryWorker>.Instance,
            Options.Create(new DiscoveryWorkerOptions { Interval = TimeSpan.FromHours(1) }));

        await worker.StartAsync(CancellationToken.None);
        await Task.Delay(200);
        await worker.StopAsync(CancellationToken.None);

        instanceRepoMock.Verify(r => r.GetAllAsync(It.IsAny<CancellationToken>()), Times.AtMostOnce());
    }

    [Fact]
    public async Task ExecuteAsync_Should_Use_Default_Interval_When_Options_Not_Provided()
    {
        var instanceRepoMock = new Mock<IInstanceRepository>();
        instanceRepoMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Instance>());

        var discoveryService = new DiscoveryService(
            instanceRepoMock.Object,
            new Mock<IDiscoveryRepository>().Object,
            NullLogger<DiscoveryService>.Instance);

        var rootProvider = CreateWorkerEnvironment(discoveryService);

        var worker = new DiscoveryWorker(
            rootProvider.Object,
            NullLogger<DiscoveryWorker>.Instance);

        await worker.StartAsync(CancellationToken.None);
        await Task.Delay(200);
        await worker.StopAsync(CancellationToken.None);

        instanceRepoMock.Verify(r => r.GetAllAsync(It.IsAny<CancellationToken>()), Times.AtMostOnce());
    }

    private static Mock<IServiceProvider> CreateWorkerEnvironment(DiscoveryService discoveryService)
    {
        var serviceScopeMock = new Mock<IServiceScope>();
        var rootProvider = new Mock<IServiceProvider>();
        var scopeProvider = new Mock<IServiceProvider>();
        var scopeFactoryMock = new Mock<IServiceScopeFactory>();

        rootProvider.Setup(p => p.GetService(typeof(IServiceScopeFactory)))
            .Returns(scopeFactoryMock.Object);
        scopeFactoryMock.Setup(f => f.CreateScope())
            .Returns(serviceScopeMock.Object);
        serviceScopeMock.Setup(s => s.ServiceProvider)
            .Returns(scopeProvider.Object);
        scopeProvider.Setup(p => p.GetService(typeof(DiscoveryService)))
            .Returns(discoveryService);

        return rootProvider;
    }
}
