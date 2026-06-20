using DbTraffic.Core.Entities;
using DbTraffic.Core.Enums;
using DbTraffic.Core.Exceptions;

namespace DbTraffic.Core.Tests;

public class EntityValidationTests
{
    [Fact]
    public void Instance_Validate_Should_Throw_When_Name_Is_Empty()
    {
        var instance = new Instance { ConnectionString = "Server=." };

        var exception = Assert.Throws<DomainException>(() => instance.Validate());

        Assert.Equal("Instance name is required.", exception.Message);
    }

    [Fact]
    public void Instance_Validate_Should_Throw_When_ConnectionString_Is_Empty()
    {
        var instance = new Instance { Name = "Test" };

        var exception = Assert.Throws<DomainException>(() => instance.Validate());

        Assert.Equal("Connection string is required.", exception.Message);
    }

    [Fact]
    public void Instance_Validate_Should_Not_Throw_When_Valid()
    {
        var instance = new Instance { Name = "Test", ConnectionString = "Server=." };

        var exception = Record.Exception(() => instance.Validate());

        Assert.Null(exception);
    }

    [Fact]
    public void Process_Validate_Should_Throw_When_Name_Is_Empty()
    {
        var process = new Process { InstanceId = Guid.NewGuid() };

        var exception = Assert.Throws<DomainException>(() => process.Validate());

        Assert.Equal("Process name is required.", exception.Message);
    }

    [Fact]
    public void Process_Validate_Should_Throw_When_InstanceId_Is_Empty()
    {
        var process = new Process { Name = "Test" };

        var exception = Assert.Throws<DomainException>(() => process.Validate());

        Assert.Equal("Instance is required.", exception.Message);
    }

    [Fact]
    public void Process_Validate_Should_Throw_When_Duration_Is_Not_Positive()
    {
        var process = new Process
        {
            Name = "Test",
            InstanceId = Guid.NewGuid(),
            EstimatedDurationMinutes = 0
        };

        var exception = Assert.Throws<DomainException>(() => process.Validate());

        Assert.Equal("Estimated duration must be greater than zero.", exception.Message);
    }

    [Fact]
    public void Process_Validate_Should_Throw_When_Window_End_Is_Before_Start()
    {
        var process = new Process
        {
            Name = "Test",
            InstanceId = Guid.NewGuid(),
            PreferredWindowStart = TimeSpan.FromHours(10),
            PreferredWindowEnd = TimeSpan.FromHours(9)
        };

        var exception = Assert.Throws<DomainException>(() => process.Validate());

        Assert.Equal("Preferred window end must be after start.", exception.Message);
    }

    [Fact]
    public void Process_Validate_Should_Not_Throw_When_Valid()
    {
        var process = new Process
        {
            Name = "Test",
            InstanceId = Guid.NewGuid(),
            ProcessType = ProcessType.SqlAgentJob,
            CpuIntensity = IntensityLevel.Medium
        };

        var exception = Record.Exception(() => process.Validate());

        Assert.Null(exception);
    }
}
