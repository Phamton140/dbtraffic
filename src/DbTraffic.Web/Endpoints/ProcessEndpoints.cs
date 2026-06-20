using DbTraffic.Core.Entities;
using DbTraffic.Core.Repositories;

namespace DbTraffic.Web.Endpoints;

public static class ProcessEndpoints
{
    public static IEndpointRouteBuilder MapProcessEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/processes");

        group.MapGet("/", async (IProcessRepository repository, CancellationToken cancellationToken) =>
        {
            var processes = await repository.GetAllAsync(cancellationToken);
            return Results.Ok(processes);
        });

        group.MapGet("/{id:guid}", async (Guid id, IProcessRepository repository, CancellationToken cancellationToken) =>
        {
            var process = await repository.GetByIdAsync(id, cancellationToken);
            return process is null ? Results.NotFound() : Results.Ok(process);
        });

        group.MapGet("/instance/{instanceId:guid}", async (Guid instanceId, IProcessRepository repository, CancellationToken cancellationToken) =>
        {
            var processes = await repository.GetByInstanceIdAsync(instanceId, cancellationToken);
            return Results.Ok(processes);
        });

        group.MapPost("/", async (Process process, IProcessRepository repository, CancellationToken cancellationToken) =>
        {
            var created = await repository.CreateAsync(process, cancellationToken);
            return Results.Created($"/api/processes/{created.Id}", created);
        });

        group.MapPut("/{id:guid}", async (Guid id, Process process, IProcessRepository repository, CancellationToken cancellationToken) =>
        {
            if (id != process.Id)
            {
                return Results.BadRequest("Id mismatch.");
            }

            await repository.UpdateAsync(process, cancellationToken);
            return Results.NoContent();
        });

        group.MapDelete("/{id:guid}", async (Guid id, IProcessRepository repository, CancellationToken cancellationToken) =>
        {
            await repository.DeleteAsync(id, cancellationToken);
            return Results.NoContent();
        });

        return app;
    }
}
