using DbTraffic.Core.Services;
using Microsoft.AspNetCore.Mvc;

namespace DbTraffic.Web.Endpoints;

public static class RecommendationEndpoints
{
    public static IEndpointRouteBuilder MapRecommendationEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/recommendations");

        group.MapGet("/", async (
            [FromQuery] Guid processId,
            [FromQuery] DateTime? searchStart,
            [FromQuery] DateTime? searchEnd,
            [FromQuery] int? granularityMinutes,
            [FromQuery] int? maxRecommendations,
            IRecommendationService recommendationService,
            CancellationToken cancellationToken) =>
        {
            var start = searchStart ?? DateTime.UtcNow;
            var end = searchEnd ?? start.AddDays(3);
            var request = new RecommendationRequest
            {
                ProcessId = processId,
                SearchStart = start,
                SearchEnd = end,
                GranularityMinutes = granularityMinutes ?? 30,
                MaxRecommendations = maxRecommendations ?? 5
            };

            var windows = await recommendationService.FindWindowsAsync(request, cancellationToken);
            return Results.Ok(windows);
        });

        return app;
    }
}
