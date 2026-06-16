using BornoBit.Restaurant.Application.Inventory.Items;
using BornoBit.Restaurant.Application.Inventory.Purchases;
using BornoBit.Restaurant.Application.Ordering.Queries;
using BornoBit.Restaurant.Application.Store.Issues;
using BornoBit.Restaurant.Application.Store.Items;
using BornoBit.Restaurant.Application.Store.Ledger;
using BornoBit.Restaurant.Application.Store.Purchases;
using BornoBit.Restaurant.Application.Store.Reports;
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

        // ---- Store (warehouse) reports ----

        // Store goods-receipt print (reuses the goods-receipt document).
        app.MapGet("/reports/store/grn/{id:guid}/receipt.pdf", async (
            Guid id, ISender sender, IReportRenderer renderer,
            IOptions<ReceiptBranding> branding, CancellationToken ct) =>
        {
            try
            {
                var grn = await sender.Send(new GetStoreGoodsReceiptQuery(id), ct);
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
        }).RequireAuthorization("Store");

        // Store issue voucher.
        app.MapGet("/reports/store/issue/{id:guid}/voucher.pdf", async (
            Guid id, ISender sender, IReportRenderer renderer,
            IOptions<ReceiptBranding> branding, CancellationToken ct) =>
        {
            try
            {
                var issue = await sender.Send(new GetStoreIssueQuery(id), ct);
                var data = new StoreIssueVoucherReportData(
                    RestaurantName: branding.Value.Name,
                    IssueNumber: issue.IssueNumber,
                    Destination: issue.Destination,
                    IssuedAtUtc: issue.IssuedAtUtc,
                    Status: issue.Status.ToString(),
                    Notes: issue.Notes,
                    GeneratedAtUtc: DateTime.UtcNow,
                    Lines: issue.Lines
                        .Select(l => new StoreIssueVoucherLine(l.ItemName, l.Qty, l.UnitCode))
                        .ToList(),
                    RequisitionNumber: issue.RequisitionNumber);

                var pdf = await renderer.RenderStoreIssueVoucherAsync(data, ct);
                return Results.File(pdf, "application/pdf", $"{issue.IssueNumber}.pdf");
            }
            catch (NotFoundException ex)
            {
                return Results.NotFound(new { message = ex.Message });
            }
        }).RequireAuthorization("Store");

        // Store stock valuation (also serves the low-stock report via lowStockOnly=true).
        app.MapGet("/reports/store/valuation.pdf", async (
            string? search, Guid? categoryId, bool? lowStockOnly, bool? includeInactive,
            ISender sender, IReportRenderer renderer, IOptions<ReceiptBranding> branding, CancellationToken ct) =>
        {
            var result = await sender.Send(new GetStoreItemsQuery(
                Search: search,
                CategoryId: categoryId,
                LowStockOnly: lowStockOnly ?? false,
                IncludeInactive: includeInactive ?? false,
                Page: 1, PageSize: 1000), ct);

            var lines = result.Items
                .Select(i => new StockValuationLine(
                    i.CategoryName, i.Code, i.Name,
                    i.IsPerishable ? "Perishable" : "Standard",
                    i.QtyOnHand, i.UnitCode, i.ReorderLevel, i.AvgCost, i.StockValue, i.IsLowStock))
                .ToList();

            var filters = new List<string>();
            if (!string.IsNullOrWhiteSpace(search)) filters.Add($"search=\"{search}\"");
            if (lowStockOnly == true) filters.Add("low stock only");
            if (includeInactive == true) filters.Add("incl. inactive");

            var data = new StockValuationReportData(
                RestaurantName: branding.Value.Name,
                GeneratedAtUtc: DateTime.UtcNow,
                Currency: result.Items.Select(i => i.Currency).FirstOrDefault() ?? "Tk",
                GrandTotal: lines.Sum(l => l.StockValue),
                Lines: lines,
                FilterNote: filters.Count > 0 ? "Store · " + string.Join(", ", filters) : "Store");

            var pdf = await renderer.RenderStockValuationAsync(data, ct);
            return Results.File(pdf, "application/pdf", lowStockOnly == true ? "store-low-stock.pdf" : "store-valuation.pdf");
        }).RequireAuthorization("Store");

        // Store stock-movement ledger (running balance when itemId is set).
        app.MapGet("/reports/store/ledger.pdf", async (
            Guid? itemId, DateTime? fromUtc, DateTime? toUtc,
            ISender sender, IReportRenderer renderer, IOptions<ReceiptBranding> branding, CancellationToken ct) =>
        {
            var ledger = await sender.Send(new GetStoreMovementLedgerQuery(itemId, fromUtc, toUtc), ct);

            var data = new StoreMovementLedgerReportData(
                RestaurantName: branding.Value.Name,
                ItemName: ledger.ItemName,
                FromUtc: ledger.FromUtc,
                ToUtc: ledger.ToUtc,
                OpeningBalance: ledger.OpeningBalance,
                ClosingBalance: ledger.ClosingBalance,
                UnitCode: ledger.UnitCode,
                GeneratedAtUtc: DateTime.UtcNow,
                Lines: ledger.Rows
                    .Select(r => new StoreMovementLedgerLine(
                        r.OccurredAtUtc, r.ItemName, r.UnitCode, r.MovementType.ToString(),
                        r.QtyBase, r.Reason, r.RunningBalance))
                    .ToList());

            var pdf = await renderer.RenderStoreMovementLedgerAsync(data, ct);
            return Results.File(pdf, "application/pdf", "store-ledger.pdf");
        }).RequireAuthorization("Store");

        // Department-wise consumption report (what the store issued to each department over a date range).
        app.MapGet("/reports/store/department-issues.pdf", async (
            DateTime? fromUtc, DateTime? toUtc, Guid? departmentId,
            ISender sender, IReportRenderer renderer, IOptions<ReceiptBranding> branding, CancellationToken ct) =>
        {
            var from = (fromUtc ?? DateTime.UtcNow.Date).Date;
            var to = (toUtc ?? DateTime.UtcNow.Date).Date.AddDays(1);

            var result = await sender.Send(new GetStoreDepartmentConsumptionQuery(from, to, departmentId), ct);

            var data = new StoreDepartmentConsumptionReportData(
                RestaurantName: branding.Value.Name,
                FromUtc: result.FromUtc,
                ToUtc: result.ToUtc,
                Currency: "Tk",
                GrandTotalValue: result.GrandTotalValue,
                GeneratedAtUtc: DateTime.UtcNow,
                Rows: result.Rows
                    .Select(r => new StoreDepartmentConsumptionReportRow(
                        r.DepartmentName, r.TotalValue,
                        r.Items.Select(i => new StoreDepartmentConsumptionReportItem(i.ItemName, i.BaseUnitCode, i.QtyBase, i.Value)).ToList()))
                    .ToList());

            var pdf = await renderer.RenderStoreDepartmentConsumptionAsync(data, ct);
            return Results.File(pdf, "application/pdf", "store-department-consumption.pdf");
        }).RequireAuthorization("Store");

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
