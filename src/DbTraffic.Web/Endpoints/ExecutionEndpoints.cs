using DbTraffic.Core.Entities;
using DbTraffic.Core.Services;
using Microsoft.AspNetCore.Mvc;

namespace DbTraffic.Web.Endpoints;

public static class ExecutionEndpoints
{
    public static IEndpointRouteBuilder MapExecutionEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/executions");

        group.MapGet("/", async (IExecutionService executionService, CancellationToken cancellationToken) =>
        {
            var executions = await executionService.GetAllAsync(cancellationToken);
            return Results.Ok(executions);
        });

        group.MapGet("/{id:guid}", async (Guid id, IExecutionService executionService, CancellationToken cancellationToken) =>
        {
            var execution = await executionService.GetByIdAsync(id, cancellationToken);
            return execution is null ? Results.NotFound() : Results.Ok(execution);
        });

        group.MapPost("/", async (Execution execution, IExecutionService executionService, CancellationToken cancellationToken) =>
        {
            var created = await executionService.CreateAsync(execution, cancellationToken);
            return Results.Created($"/api/executions/{created.Id}", created);
        });

        group.MapPost("/{id:guid}/complete", async (
            Guid id,
            [FromQuery] DateTime completedAt,
            [FromQuery] string status,
            IExecutionService executionService,
            CancellationToken cancellationToken) =>
        {
            var completed = await executionService.CompleteAsync(id, completedAt, status, cancellationToken);
            return Results.Ok(completed);
        });

        group.MapDelete("/{id:guid}", async (Guid id, IExecutionService executionService, CancellationToken cancellationToken) =>
        {
            await executionService.DeleteAsync(id, cancellationToken);
            return Results.NoContent();
        });

        group.MapPost("/import/{instanceId:guid}", async (
            Guid instanceId,
            [FromQuery] DateTime? since,
            IExecutionService executionService,
            CancellationToken cancellationToken) =>
        {
            var importSince = since ?? DateTime.UtcNow.AddDays(-30);
            var count = await executionService.ImportFromInstanceAsync(instanceId, importSince, cancellationToken);
            return Results.Ok(new { ImportedCount = count });
        });

        group.MapPost("/processes/{processId:guid}/calibrate", async (
            Guid processId,
            IExecutionService executionService,
            CancellationToken cancellationToken) =>
        {
            await executionService.CalibrateProcessDurationAsync(processId, cancellationToken);
            return Results.NoContent();
        });

        return app;
    }
}
