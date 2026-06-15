using BornoBit.Restaurant.Application.Ordering.Commands;
using BornoBit.Restaurant.Application.Ordering.Queries;
using BornoBit.Restaurant.Domain.Ordering;
using BornoBit.Restaurant.Shared.Common;
using MediatR;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace BornoBit.Restaurant.Api.Endpoints;

public static class OrderEndpoints
{
    public static IEndpointRouteBuilder MapOrderEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/orders")
            .RequireCors("Frontends")
            .RequireAuthorization("Customer")
            .WithTags("Orders");

        group.MapPost("", async (PlaceOrderRequest body, ClaimsPrincipal user, ISender sender, CancellationToken ct) =>
        {
            var customerId = GetCustomerId(user);
            if (customerId is null) return Results.Unauthorized();

            var lines = (body.Lines ?? new List<PlaceOrderLineRequest>())
                .Select(l => new PlaceOrderLineInput(l.MenuItemId, l.Quantity, l.VariantId, null,
                    l.OptionIds is { Count: > 0 } ? l.OptionIds : null))
                .ToList();

            try
            {
                var result = await sender.Send(new PlaceOrderCommand(customerId.Value, body.TableId, body.Type, body.Notes, lines), ct);
                return Results.Created($"/orders/{result.OrderId}", result);
            }
            catch (NotFoundException ex)
            {
                return Results.NotFound(new { message = ex.Message });
            }
            catch (ConflictException ex)
            {
                return Results.Conflict(new { message = ex.Message });
            }
        });

        group.MapGet("/mine", async (ClaimsPrincipal user, ISender sender, int? page, int? pageSize, CancellationToken ct) =>
        {
            var customerId = GetCustomerId(user);
            if (customerId is null) return Results.Unauthorized();

            var result = await sender.Send(new GetOrdersQuery(
                Page: page is > 0 ? page.Value : 1,
                PageSize: pageSize is > 0 ? pageSize.Value : 50,
                CustomerId: customerId.Value), ct);
            return Results.Ok(result);
        });

        group.MapGet("/{id:guid}", async (Guid id, ClaimsPrincipal user, ISender sender, CancellationToken ct) =>
        {
            var customerId = GetCustomerId(user);
            if (customerId is null) return Results.Unauthorized();

            try
            {
                var order = await sender.Send(new GetOrderQuery(id), ct);
                if (order.CustomerId != customerId.Value) return Results.Forbid();
                return Results.Ok(order);
            }
            catch (NotFoundException ex)
            {
                return Results.NotFound(new { message = ex.Message });
            }
        });

        return app;
    }

    private static Guid? GetCustomerId(ClaimsPrincipal user)
    {
        var raw = user.FindFirstValue(JwtRegisteredClaimNames.Sub)
                  ?? user.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(raw, out var id) ? id : null;
    }

    public record PlaceOrderLineRequest(Guid MenuItemId, int Quantity, Guid? VariantId = null, List<Guid>? OptionIds = null);
    public record PlaceOrderRequest(Guid? TableId, OrderType Type, string? Notes, List<PlaceOrderLineRequest>? Lines);
}
