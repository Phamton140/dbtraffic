using DbTraffic.Core.Services;
using Microsoft.AspNetCore.Mvc;

namespace DbTraffic.Web.Endpoints;

public static class MonitoringEndpoints
{
    public static IEndpointRouteBuilder MapMonitoringEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/monitoring");

        group.MapGet("/instances/{instanceId:guid}/snapshot", async (
            Guid instanceId,
            IMonitoringService monitoringService,
            CancellationToken cancellationToken) =>
        {
            var snapshot = await monitoringService.CaptureSnapshotAsync(instanceId, cancellationToken);
            return Results.Ok(snapshot);
        });

        group.MapGet("/instances/{instanceId:guid}/metrics", async (
            Guid instanceId,
            IMonitoringService monitoringService,
            CancellationToken cancellationToken) =>
        {
            var metrics = await monitoringService.GetCurrentMetricsAsync(instanceId, cancellationToken);
            return Results.Ok(metrics);
        });

        group.MapGet("/instances/{instanceId:guid}/active-requests", async (
            Guid instanceId,
            IMonitoringService monitoringService,
            CancellationToken cancellationToken) =>
        {
            var requests = await monitoringService.GetActiveRequestsAsync(instanceId, cancellationToken);
            return Results.Ok(requests);
        });

        return app;
    }
}
