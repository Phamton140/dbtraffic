using DbTraffic.Core.Repositories;
using DbTraffic.Infrastructure.Discovery;

namespace DbTraffic.Web.Endpoints;

public static class DiscoveryEndpoints
{
    public static IEndpointRouteBuilder MapDiscoveryEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/discovery");

        group.MapPost("/run/{instanceId:guid}", async (Guid instanceId, DiscoveryService discoveryService, CancellationToken cancellationToken) =>
        {
            await discoveryService.DiscoverInstanceAsync(instanceId, cancellationToken);
            return Results.NoContent();
        });

        group.MapPost("/run-all", async (DiscoveryService discoveryService, CancellationToken cancellationToken) =>
        {
            await discoveryService.DiscoverAllActiveInstancesAsync(cancellationToken);
            return Results.NoContent();
        });

        group.MapGet("/jobs/{instanceId:guid}", async (Guid instanceId, IDiscoveryRepository repository, CancellationToken cancellationToken) =>
        {
            var jobs = await repository.GetJobsByInstanceAsync(instanceId, cancellationToken);
            return Results.Ok(jobs);
        });

        group.MapGet("/objects/{instanceId:guid}", async (Guid instanceId, IDiscoveryRepository repository, CancellationToken cancellationToken) =>
        {
            var objects = await repository.GetObjectsByInstanceAsync(instanceId, cancellationToken);
            return Results.Ok(objects);
        });

        group.MapPost("/associate/{discoveredJobId:guid}", async (Guid discoveredJobId, AssociateRequest request, IDiscoveryRepository repository, CancellationToken cancellationToken) =>
        {
            await repository.AssociateJobAsync(discoveredJobId, request.ProcessId, cancellationToken);
            return Results.NoContent();
        });

        return app;
    }
}

public sealed class AssociateRequest
{
    public Guid? ProcessId { get; set; }
}
