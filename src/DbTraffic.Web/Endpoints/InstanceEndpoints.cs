using DbTraffic.Core.Entities;
using DbTraffic.Core.Repositories;

namespace DbTraffic.Web.Endpoints;

public static class InstanceEndpoints
{
    public static IEndpointRouteBuilder MapInstanceEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/instances");

        group.MapGet("/", async (IInstanceRepository repository, CancellationToken cancellationToken) =>
        {
            var instances = await repository.GetAllAsync(cancellationToken);
            return Results.Ok(instances);
        });

        group.MapGet("/{id:guid}", async (Guid id, IInstanceRepository repository, CancellationToken cancellationToken) =>
        {
            var instance = await repository.GetByIdAsync(id, cancellationToken);
            return instance is null ? Results.NotFound() : Results.Ok(instance);
        });

        group.MapPost("/", async (Instance instance, IInstanceRepository repository, CancellationToken cancellationToken) =>
        {
            var created = await repository.CreateAsync(instance, cancellationToken);
            return Results.Created($"/api/instances/{created.Id}", created);
        });

        group.MapPut("/{id:guid}", async (Guid id, Instance instance, IInstanceRepository repository, CancellationToken cancellationToken) =>
        {
            if (id != instance.Id)
            {
                return Results.BadRequest("Id mismatch.");
            }

            await repository.UpdateAsync(instance, cancellationToken);
            return Results.NoContent();
        });

        group.MapDelete("/{id:guid}", async (Guid id, IInstanceRepository repository, CancellationToken cancellationToken) =>
        {
            await repository.DeleteAsync(id, cancellationToken);
            return Results.NoContent();
        });

        return app;
    }
}
