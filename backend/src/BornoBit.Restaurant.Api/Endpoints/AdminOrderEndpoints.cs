using BornoBit.Restaurant.Application.Ordering.Queries;
using BornoBit.Restaurant.Domain.Ordering;
using BornoBit.Restaurant.Shared.Common;
using MediatR;

namespace BornoBit.Restaurant.Api.Endpoints;

public static class AdminOrderEndpoints
{
    public static IEndpointRouteBuilder MapAdminOrderEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/admin/orders")
            .RequireCors("Frontends")
            .RequireAuthorization("Staff")
            .WithTags("AdminOrders");

        group.MapGet("", async (ISender sender, OrderStatus? status, int? page, int? pageSize, CancellationToken ct) =>
        {
            var result = await sender.Send(new GetOrdersQuery(
                Status: status,
                Page: page is > 0 ? page.Value : 1,
                PageSize: pageSize is > 0 ? pageSize.Value : 50), ct);
            return Results.Ok(result);
        });

        group.MapGet("/{id:guid}", async (Guid id, ISender sender, CancellationToken ct) =>
        {
            try
            {
                var order = await sender.Send(new GetOrderQuery(id), ct);
                return Results.Ok(order);
            }
            catch (NotFoundException ex)
            {
                return Results.NotFound(new { message = ex.Message });
            }
        });

        return app;
    }
}
