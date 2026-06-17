using BornoBit.Restaurant.Application.Operations.Dashboard;
using BornoBit.Restaurant.Application.Ordering.Queries;
using BornoBit.Restaurant.Domain.Dining;
using BornoBit.Restaurant.Domain.Ordering;
using MediatR;

namespace BornoBit.Restaurant.Api.Endpoints;

public static class DashboardEndpoints
{
    public static IEndpointRouteBuilder MapDashboardEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/staff/dashboard")
            .RequireCors("Frontends")
            .RequireAuthorization("Staff")
            .WithTags("Dashboard");

        // ---- Live sections ----
        group.MapGet("/summary", async (ISender sender, CancellationToken ct) =>
            Results.Ok(await sender.Send(new GetDashboardSummaryQuery(), ct)));

        group.MapGet("/tables", async (ISender sender, CancellationToken ct) =>
            Results.Ok(await sender.Send(new GetTableOverviewQuery(), ct)));

        group.MapGet("/kitchen", async (ISender sender, CancellationToken ct) =>
            Results.Ok(await sender.Send(new GetKitchenPerformanceQuery(), ct)));

        group.MapGet("/orders", async (ISender sender, OrderStatus? status, int? page, int? pageSize, CancellationToken ct) =>
            Results.Ok(await sender.Send(new GetLiveOrdersQuery(
                status,
                page is > 0 ? page.Value : 1,
                pageSize is > 0 ? pageSize.Value : 20), ct)));

        group.MapGet("/requests", async (ISender sender, CustomerRequestStatus? status, CancellationToken ct) =>
            Results.Ok(await sender.Send(new GetCustomerRequestsQuery(status ?? CustomerRequestStatus.Pending), ct)));

        group.MapGet("/inventory-alerts", async (ISender sender, CancellationToken ct) =>
            Results.Ok(await sender.Send(new GetInventoryAlertsQuery(), ct)));

        group.MapGet("/staff-activity", async (ISender sender, CancellationToken ct) =>
            Results.Ok(await sender.Send(new GetStaffActivityQuery(), ct)));

        // ---- Analytics sections (date-range driven; defaults to today UTC) ----
        group.MapGet("/sales-by-hour", async (ISender sender, DateTime? from, DateTime? to, CancellationToken ct) =>
        {
            var (f, t) = Window(from, to);
            return Results.Ok(await sender.Send(new GetSalesByHourQuery(f, t), ct));
        });

        group.MapGet("/sales-by-category", async (ISender sender, DateTime? from, DateTime? to, CancellationToken ct) =>
        {
            var (f, t) = Window(from, to);
            return Results.Ok(await sender.Send(new GetSalesByCategoryQuery(f, t), ct));
        });

        group.MapGet("/top-items", async (ISender sender, DateTime? from, DateTime? to, int? count, CancellationToken ct) =>
        {
            var (f, t) = Window(from, to);
            return Results.Ok(await sender.Send(new GetTopSellingItemsQuery(f, t, count is > 0 ? count.Value : 8), ct));
        });

        group.MapGet("/revenue-breakdown", async (ISender sender, DateTime? from, DateTime? to, CancellationToken ct) =>
        {
            var (f, t) = Window(from, to);
            return Results.Ok(await sender.Send(new GetRevenueBreakdownQuery(f, t), ct));
        });

        return app;
    }

    private static (DateTime From, DateTime To) Window(DateTime? from, DateTime? to)
    {
        var today = DateTime.UtcNow.Date;
        return (from?.Date ?? today, to?.Date ?? today);
    }
}
