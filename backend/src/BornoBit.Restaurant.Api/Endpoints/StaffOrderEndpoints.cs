using BornoBit.Restaurant.Application.Ordering.Queries;
using BornoBit.Restaurant.Domain.Ordering;
using BornoBit.Restaurant.Shared.Common;
using MediatR;

namespace BornoBit.Restaurant.Api.Endpoints;

/// <summary>
/// Staff Orders module over HTTP — paged list + detail, reusing the existing
/// Application queries (also used by the legacy /admin/orders endpoints).
/// Mounted under the versioned group → /api/v1/staff/orders.
/// </summary>
public static class StaffOrderEndpoints
{
    public static IEndpointRouteBuilder MapStaffOrderEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/staff/orders")
            .RequireCors("Frontends")
            .RequireAuthorization("Staff")
            .WithTags("StaffOrders");

        group.MapGet("", async (
            ISender sender,
            OrderStatus? status,
            bool? isPaid,
            bool? excludeCancelled,
            DateOnly? from,
            DateOnly? to,
            string? search,
            string? orderNumber,
            int? page,
            int? pageSize,
            CancellationToken ct) =>
        {
            var result = await sender.Send(new GetOrdersQuery(
                Status: status,
                Page: page is > 0 ? page.Value : 1,
                PageSize: pageSize is > 0 ? pageSize.Value : 25,
                IsPaid: isPaid,
                ExcludeCancelled: excludeCancelled ?? false,
                From: from,
                To: to,
                Search: search,
                OrderNumber: orderNumber), ct);
            return Results.Ok(result);
        });

        group.MapGet("/summary", async (
            ISender sender,
            DateOnly? from,
            DateOnly? to,
            string? search,
            string? orderNumber,
            CancellationToken ct) =>
        {
            var result = await sender.Send(new GetOrdersSummaryQuery(from, to, search, orderNumber), ct);
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
