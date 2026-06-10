using BornoBit.Restaurant.Application.Inventory.Items;
using BornoBit.Restaurant.Application.Ordering.Queries;
using BornoBit.Restaurant.Reporting;
using BornoBit.Restaurant.Reporting.Models;
using BornoBit.Restaurant.Shared.Common;
using MediatR;

namespace BornoBit.Restaurant.Web.Endpoints;

public static class ReportEndpoints
{
    private const string RestaurantName = "BornoBit Restaurant";

    public static IEndpointRouteBuilder MapReportEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/reports/order/{id:guid}/receipt.pdf", async (
            Guid id, ISender sender, IReportRenderer renderer, CancellationToken ct) =>
        {
            try
            {
                var order = await sender.Send(new GetOrderQuery(id), ct);
                var data = new OrderReceiptReportData(
                    RestaurantName: RestaurantName,
                    OrderNumber: order.OrderNumber,
                    OrderedAtUtc: order.OrderedAtUtc,
                    OrderType: order.OrderType.ToString(),
                    Status: order.Status.ToString(),
                    TableNumber: order.TableNumber,
                    CustomerName: order.CustomerName,
                    CustomerPhone: order.CustomerPhone,
                    Currency: order.Currency,
                    Subtotal: order.Subtotal,
                    DiscountAmount: order.DiscountAmount,
                    Total: order.Total,
                    IsPaid: order.IsPaid,
                    PaymentMethod: order.PaymentMethod?.ToString(),
                    AmountTendered: order.AmountTendered,
                    ChangeGiven: order.ChangeGiven,
                    Notes: order.Notes,
                    GeneratedAtUtc: DateTime.UtcNow,
                    Lines: order.Lines.Select(l => new OrderReceiptLine(l.Code, l.Name, l.Quantity, l.UnitPrice, l.LineTotal)).ToList());

                var pdf = await renderer.RenderOrderReceiptAsync(data, ct);
                return Results.File(pdf, "application/pdf", $"{order.OrderNumber}.pdf");
            }
            catch (NotFoundException ex)
            {
                return Results.NotFound(new { message = ex.Message });
            }
        }).RequireAuthorization("Staff");

        app.MapGet("/reports/stock/valuation.pdf", async (
            ISender sender, IReportRenderer renderer, CancellationToken ct) =>
        {
            var result = await sender.Send(new GetInventoryItemsQuery(PageSize: 1000), ct);
            var lines = result.Items
                .Where(i => i.IsActive)
                .Select(i => new StockValuationLine(
                    i.CategoryName, i.Code, i.Name, i.QtyOnHand, i.UnitCode, i.AvgCost, i.StockValue, i.IsLowStock))
                .ToList();

            var data = new StockValuationReportData(
                RestaurantName: RestaurantName,
                GeneratedAtUtc: DateTime.UtcNow,
                Currency: "Tk",
                GrandTotal: lines.Sum(l => l.StockValue),
                Lines: lines);

            var pdf = await renderer.RenderStockValuationAsync(data, ct);
            return Results.File(pdf, "application/pdf", "stock-valuation.pdf");
        }).RequireAuthorization("Inventory");

        return app;
    }
}
