using BornoBit.Restaurant.Application.Inventory.Items;
using BornoBit.Restaurant.Application.Inventory.Purchases;
using BornoBit.Restaurant.Application.Ordering.Queries;
using BornoBit.Restaurant.Domain.Inventory;
using BornoBit.Restaurant.Reporting;
using BornoBit.Restaurant.Reporting.Models;
using BornoBit.Restaurant.Shared.Common;
using BornoBit.Restaurant.Web.Services.Printing;
using MediatR;
using Microsoft.Extensions.Options;
using System.Security.Claims;

namespace BornoBit.Restaurant.Web.Endpoints;

public static class ReportEndpoints
{
    public static IEndpointRouteBuilder MapReportEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/reports/order/{id:guid}/receipt.pdf", async (
            Guid id, ClaimsPrincipal user, ISender sender, IReportRenderer renderer,
            IOptions<ReceiptBranding> branding, CancellationToken ct) =>
        {
            try
            {
                var order = await sender.Send(new GetOrderQuery(id), ct);
                var pdf = await renderer.RenderOrderReceiptAsync(ToReceiptData(order, branding.Value, user), ct);
                return Results.File(pdf, "application/pdf", $"{order.OrderNumber}.pdf");
            }
            catch (NotFoundException ex)
            {
                return Results.NotFound(new { message = ex.Message });
            }
        }).RequireAuthorization("Staff");

        app.MapGet("/reports/order/{id:guid}/pos-receipt.pdf", async (
            Guid id, ClaimsPrincipal user, ISender sender, IReportRenderer renderer,
            IOptions<ReceiptBranding> branding, CancellationToken ct) =>
        {
            try
            {
                var order = await sender.Send(new GetOrderQuery(id), ct);
                var pdf = await renderer.RenderPosReceiptAsync(ToReceiptData(order, branding.Value, user), ct);
                return Results.File(pdf, "application/pdf", $"{order.OrderNumber}-pos.pdf");
            }
            catch (NotFoundException ex)
            {
                return Results.NotFound(new { message = ex.Message });
            }
        }).RequireAuthorization("Staff");

        app.MapGet("/reports/order/{id:guid}/kot.pdf", async (
            Guid id, ClaimsPrincipal user, ISender sender, IReportRenderer renderer,
            IOptions<ReceiptBranding> branding, CancellationToken ct) =>
        {
            try
            {
                var order = await sender.Send(new GetOrderQuery(id), ct);
                var pdf = await renderer.RenderKitchenTicketAsync(ToKitchenTicketData(order, branding.Value, user), ct);
                return Results.File(pdf, "application/pdf", $"{order.OrderNumber}-kot.pdf");
            }
            catch (NotFoundException ex)
            {
                return Results.NotFound(new { message = ex.Message });
            }
        }).RequireAuthorization("Staff");

        // Sends the receipt to the thermal print agent (reprint by default).
        app.MapPost("/print/order/{id:guid}", async (
            Guid id, bool? reprint, IReceiptPrintService printService, CancellationToken ct) =>
        {
            var result = await printService.PrintReceiptAsync(id, isReprint: reprint ?? true, ct);
            return result.Success
                ? Results.Ok(result)
                : Results.Problem(result.Message, statusCode: StatusCodes.Status502BadGateway);
        }).RequireAuthorization("Staff");

        app.MapGet("/reports/stock/valuation.pdf", async (
            string? search, Guid? categoryId, string? itemType, bool? lowStockOnly, bool? includeInactive,
            string? sortBy, bool? sortDesc,
            ISender sender, IReportRenderer renderer, IOptions<ReceiptBranding> branding, CancellationToken ct) =>
        {
            InventoryItemType? type = Enum.TryParse<InventoryItemType>(itemType, true, out var t) ? t : null;

            var result = await sender.Send(new GetInventoryItemsQuery(
                Search: search,
                CategoryId: categoryId,
                ItemType: type,
                LowStockOnly: lowStockOnly ?? false,
                IncludeInactive: includeInactive ?? false,
                SortBy: sortBy,
                SortDesc: sortDesc ?? false,
                Page: 1, PageSize: 1000), ct);

            var lines = result.Items
                .Select(i => new StockValuationLine(
                    i.CategoryName, i.Code, i.Name,
                    i.ItemType == InventoryItemType.Ingredient ? "Ingredient" : "Finished",
                    i.QtyOnHand, i.UnitCode, i.ReorderLevel, i.AvgCost, i.StockValue, i.IsLowStock))
                .ToList();

            var filters = new List<string>();
            if (!string.IsNullOrWhiteSpace(search)) filters.Add($"search=\"{search}\"");
            if (categoryId is not null && lines.Count > 0) filters.Add($"category={lines[0].Category}");
            if (type is not null) filters.Add($"type={type}");
            if (lowStockOnly == true) filters.Add("low stock only");
            if (includeInactive == true) filters.Add("incl. inactive");

            var data = new StockValuationReportData(
                RestaurantName: branding.Value.Name,
                GeneratedAtUtc: DateTime.UtcNow,
                Currency: "Tk",
                GrandTotal: lines.Sum(l => l.StockValue),
                Lines: lines,
                FilterNote: filters.Count > 0 ? "Filter: " + string.Join(", ", filters) : null);

            var pdf = await renderer.RenderStockValuationAsync(data, ct);
            return Results.File(pdf, "application/pdf", "stock-valuation.pdf");
        }).RequireAuthorization("Inventory");

        app.MapGet("/reports/grn/{id:guid}/receipt.pdf", async (
            Guid id, ISender sender, IReportRenderer renderer,
            IOptions<ReceiptBranding> branding, CancellationToken ct) =>
        {
            try
            {
                var grn = await sender.Send(new GetGoodsReceiptQuery(id), ct);
                var data = new GoodsReceiptReportData(
                    RestaurantName: branding.Value.Name,
                    GrnNumber: grn.GrnNumber,
                    SupplierName: grn.SupplierName,
                    InvoiceNo: grn.InvoiceNo,
                    ReceivedAtUtc: grn.ReceivedAtUtc,
                    Status: grn.Status.ToString(),
                    Currency: grn.Currency,
                    Notes: grn.Notes,
                    Subtotal: grn.Subtotal,
                    GeneratedAtUtc: DateTime.UtcNow,
                    Lines: grn.Lines
                        .Select(l => new GoodsReceiptReportLine(l.ItemName, l.Qty, l.UnitCode, l.UnitCost, l.LineTotal))
                        .ToList());

                var pdf = await renderer.RenderGoodsReceiptAsync(data, ct);
                return Results.File(pdf, "application/pdf", $"{grn.GrnNumber}.pdf");
            }
            catch (NotFoundException ex)
            {
                return Results.NotFound(new { message = ex.Message });
            }
        }).RequireAuthorization("Inventory");

        return app;
    }

    private static OrderReceiptReportData ToReceiptData(OrderDetailDto order, ReceiptBranding branding, ClaimsPrincipal user) => new(
        RestaurantName: branding.Name,
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
        Lines: order.Lines.Select(l => new OrderReceiptLine(l.Code, l.Name, l.Quantity, l.UnitPrice, l.LineTotal,
            l.Modifiers?.Select(m => new OrderReceiptModifier(m.OptionName, m.PriceDelta)).ToList())).ToList(),
        RoundingAdjustment: order.RoundingAdjustment,
        CashierName: user.Identity?.Name,
        Branding: branding);

    private static KitchenTicketReportData ToKitchenTicketData(OrderDetailDto order, ReceiptBranding branding, ClaimsPrincipal user) => new(
        OrderNumber: order.OrderNumber,
        OrderedAtUtc: order.OrderedAtUtc,
        OrderType: order.OrderType.ToString(),
        TableNumber: order.TableNumber,
        CustomerName: order.CustomerName,
        Notes: order.Notes,
        GeneratedAtUtc: DateTime.UtcNow,
        Lines: order.Lines.Select(l => new KitchenTicketLine(l.Name, l.Quantity, l.Notes,
            l.Modifiers?.Select(m => m.OptionName).ToList())).ToList(),
        CashierName: user.Identity?.Name,
        TicketLabel: $"KOT · {order.OrderNumber}",
        Branding: branding);
}
