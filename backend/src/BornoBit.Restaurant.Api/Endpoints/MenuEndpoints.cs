using BornoBit.Restaurant.Application.Catalog.Queries;
using MediatR;

namespace BornoBit.Restaurant.Api.Endpoints;

public static class MenuEndpoints
{
    public static IEndpointRouteBuilder MapMenuEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/menu")
            .RequireCors("Frontends")
            .WithTags("Menu");

        group.MapGet("", async (ISender sender, CancellationToken ct) =>
        {
            var menu = await sender.Send(new GetMenuQuery(), ct);
            return Results.Ok(menu);
        });

        return app;
    }
}
