using DbTraffic.Core.Entities;
using DbTraffic.Core.Exceptions;

namespace DbTraffic.Core.Tests.Entities;

public class InstanceTests
{
    [Fact]
    public void Validate_ValidInstance_DoesNotThrow()
    {
        var instance = new Instance
        {
            Name = "Test Instance",
            ConnectionString = "Server=.;Database=DbTraffic;Trusted_Connection=True;TrustServerCertificate=True;"
        };

        var exception = Record.Exception(instance.Validate);

        Assert.Null(exception);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_MissingName_ThrowsDomainException(string? name)
    {
        var instance = new Instance
        {
            Name = name!,
            ConnectionString = "Server=.;Database=DbTraffic;Trusted_Connection=True;"
        };

        var exception = Assert.Throws<DomainException>(instance.Validate);
        Assert.Contains("name", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_MissingConnectionString_ThrowsDomainException(string? connectionString)
    {
        var instance = new Instance
        {
            Name = "Test Instance",
            ConnectionString = connectionString!
        };

        var exception = Assert.Throws<DomainException>(instance.Validate);
        Assert.Contains("connection string", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("databaseprueba_clientes;trusted_connection")]
    [InlineData("Invalid string without server")]
    public void Validate_ConnectionStringWithoutServer_ThrowsDomainException(string connectionString)
    {
        var instance = new Instance
        {
            Name = "Test Instance",
            ConnectionString = connectionString
        };

        var exception = Assert.Throws<DomainException>(instance.Validate);
        Assert.Contains("Server", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Validate_ConnectionStringWithServerComponent_DoesNotThrowEvenIfOtherKeywordsAreMalformed()
    {
        var instance = new Instance
        {
            Name = "Test Instance",
            ConnectionString = "Server=.;Databaseprueba_clientes"
        };

        var exception = Record.Exception(instance.Validate);

        Assert.Null(exception);
    }

    [Theory]
    [InlineData("Server=.;Database=DbTraffic;Trusted_Connection=True;")]
    [InlineData("Data Source=.;Initial Catalog=DbTraffic;Integrated Security=true;")]
    public void Validate_ConnectionStringWithServerOrDataSource_DoesNotThrow(string connectionString)
    {
        var instance = new Instance
        {
            Name = "Test Instance",
            ConnectionString = connectionString
        };

        var exception = Record.Exception(instance.Validate);

        Assert.Null(exception);
    }
}
