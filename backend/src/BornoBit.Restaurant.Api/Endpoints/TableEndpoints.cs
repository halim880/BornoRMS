using BornoBit.Restaurant.Application.Dining.Queries;
using MediatR;

namespace BornoBit.Restaurant.Api.Endpoints;

public static class TableEndpoints
{
    public static IEndpointRouteBuilder MapTableEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/tables")
            .RequireCors("Frontends")
            .WithTags("Tables");

        group.MapGet("", async (ISender sender, CancellationToken ct) =>
        {
            var tables = await sender.Send(new GetTablesQuery(), ct);
            return Results.Ok(tables);
        });

        return app;
    }
}
