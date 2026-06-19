using BornoBit.Restaurant.Application.Ordering.Queries;
using BornoBit.Restaurant.Shared.Common;
using FluentValidation;
using MediatR;

namespace BornoBit.Restaurant.Api.Endpoints;

/// <summary>
/// REST surface for the Flutter Operations → Reports screens — mirrors the Blazor
/// staff console report pages (SalesReport.razor, SalesInvoiceReport.razor,
/// CollectionReport.razor, TopSellingItems.razor). Every route forwards to an
/// existing Application-layer query (registered via AddApplication()).
/// Mounted under the versioned group → /api/v1/staff/reports/*.
/// Date-range query params (from/to) are parsed as plain calendar dates, like
/// DashboardEndpoints — defaulting to today (UTC) when omitted.
/// </summary>
public static class ReportsEndpoints
{
    public static IEndpointRouteBuilder MapReportsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/staff/reports")
            .RequireCors("Frontends")
            .RequireAuthorization("Reports")
            .WithTags("Reports");

        // Paid-sales totals over a date range, broken down per day.
        group.MapGet("/sales", (ISender sender, DateTime? from, DateTime? to, CancellationToken ct) =>
            Exec(async () =>
            {
                var (f, t) = Window(from, to);
                return Results.Ok(await sender.Send(new GetSalesReportQuery(f, t), ct));
            }));

        // Paid sales over a date range, one row per invoice (order).
        group.MapGet("/sales-invoices", (ISender sender, DateTime? from, DateTime? to, CancellationToken ct) =>
            Exec(async () =>
            {
                var (f, t) = Window(from, to);
                return Results.Ok(await sender.Send(new GetSalesInvoiceReportQuery(f, t), ct));
            }));

        // Money collected over a date range, grouped by payment method.
        group.MapGet("/collection", (ISender sender, DateTime? from, DateTime? to, CancellationToken ct) =>
            Exec(async () =>
            {
                var (f, t) = Window(from, to);
                return Results.Ok(await sender.Send(new GetCollectionReportQuery(f, t), ct));
            }));

        // Top-selling menu items by quantity over a date range, from paid orders.
        group.MapGet("/top-items", (ISender sender, DateTime? from, DateTime? to, int? top, CancellationToken ct) =>
            Exec(async () =>
            {
                var (f, t) = Window(from, to);
                return Results.Ok(await sender.Send(new GetTopSellingItemsQuery(f, t, top is > 0 ? top.Value : 20), ct));
            }));

        return app;
    }

    private static (DateTime From, DateTime To) Window(DateTime? from, DateTime? to)
    {
        var today = DateTime.UtcNow.Date;
        return (from?.Date ?? today, to?.Date ?? today);
    }

    // Shared error translation so FluentValidation failures surface as 400, not 500.
    private static async Task<IResult> Exec(Func<Task<IResult>> action)
    {
        try
        {
            return await action();
        }
        catch (ValidationException ex)
        {
            var errors = ex.Errors.Select(e => e.ErrorMessage).ToList();
            return Results.BadRequest(new { message = errors.FirstOrDefault() ?? "Validation failed.", errors });
        }
        catch (NotFoundException ex)
        {
            return Results.NotFound(new { message = ex.Message });
        }
        catch (ConflictException ex)
        {
            return Results.Conflict(new { message = ex.Message });
        }
    }
}
