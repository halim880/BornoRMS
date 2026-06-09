namespace BornoBit.Restaurant.Api.Endpoints;

public static class HealthEndpoints
{
    public static IEndpointRouteBuilder MapHealthEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/health/live", () => Results.Ok(new { status = "alive", timestamp = DateTime.UtcNow }))
            .WithName("HealthLive")
            .WithTags("Health");

        app.MapGet("/health/ready", () => Results.Ok(new { status = "ready", timestamp = DateTime.UtcNow }))
            .WithName("HealthReady")
            .WithTags("Health");

        return app;
    }
}
