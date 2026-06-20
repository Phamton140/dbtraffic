using DbTraffic.Core.Services;
using Microsoft.AspNetCore.Mvc;

namespace DbTraffic.Web.Endpoints;

public static class RiskEndpoints
{
    public static IEndpointRouteBuilder MapRiskEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/risk");

        group.MapGet("/", async (
            [FromQuery] Guid processId,
            [FromQuery] DateTime? proposedStartTime,
            IRiskContextProvider contextProvider,
            IRiskCalculationService riskService,
            CancellationToken cancellationToken) =>
        {
            var startTime = proposedStartTime ?? DateTime.UtcNow;
            var context = await contextProvider.BuildContextAsync(processId, startTime, cancellationToken);

            if (context is null)
            {
                return Results.NotFound("Process not found.");
            }

            var assessment = await riskService.CalculateAsync(context, cancellationToken);
            return Results.Ok(assessment);
        });

        return app;
    }
}
