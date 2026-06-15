using BornoBit.Restaurant.Application.Dining.Queries;
using BornoBit.Restaurant.Application.Operations.Dashboard;
using BornoBit.Restaurant.Application.Operations.Sessions;
using BornoBit.Restaurant.Application.Ordering.Commands;
using BornoBit.Restaurant.Application.Ordering.Pos;
using BornoBit.Restaurant.Application.Ordering.Queries;
using BornoBit.Restaurant.Application.ProductCategories;
using BornoBit.Restaurant.Application.Products;
using BornoBit.Restaurant.Application.Users;
using BornoBit.Restaurant.Domain.Dining;
using BornoBit.Restaurant.Domain.Ordering;
using BornoBit.Restaurant.Reporting;
using BornoBit.Restaurant.Reporting.Models;
using BornoBit.Restaurant.Shared.Common;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Options;
using System.Security.Claims;

namespace BornoBit.Restaurant.Api.Endpoints;

/// <summary>
/// REST surface for the Flutter waiter app — mirrors the Blazor Waiter console (WaiterOrders.razor).
/// Every route forwards to an existing Application-layer MediatR handler; the API already registers
/// these via AddApplication()/AddInfrastructure(), and ICurrentUser reads the staff JWT so "my"
/// queries scope to the logged-in waiter. Mobile uses polling, so there is no SignalR here.
/// </summary>
public static class WaiterEndpoints
{
    public static IEndpointRouteBuilder MapWaiterEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/waiter")
            .RequireCors("Frontends")
            .RequireAuthorization("WaiterFloor")
            .WithTags("Waiter");

        // ---------- dashboard / floor reads (polled) ----------
        group.MapGet("/dashboard", (ISender sender, CancellationToken ct) =>
            Exec(async () => Results.Ok(await sender.Send(new GetWaiterDashboardQuery(), ct))));

        group.MapGet("/floor", (ISender sender, CancellationToken ct) =>
            Exec(async () => Results.Ok(await sender.Send(new GetTableOverviewQuery(), ct))));

        group.MapGet("/ready", (ISender sender, CancellationToken ct) =>
            Exec(async () => Results.Ok(await sender.Send(new GetReadyToServeQuery(), ct))));

        group.MapGet("/requests", (ISender sender, CustomerRequestStatus? status, CancellationToken ct) =>
            Exec(async () => Results.Ok(await sender.Send(
                new GetCustomerRequestsQuery(status ?? CustomerRequestStatus.Pending), ct))));

        group.MapGet("/my-sessions", (ISender sender, CancellationToken ct) =>
            Exec(async () => Results.Ok(await sender.Send(new GetMySessionsQuery(), ct))));

        group.MapGet("/sessions/{id:guid}", (Guid id, ISender sender, CancellationToken ct) =>
            Exec(async () => Results.Ok(await sender.Send(new GetSessionQuery(id), ct))));

        group.MapGet("/sessions/{id:guid}/bill", (Guid id, ISender sender, CancellationToken ct) =>
            Exec(async () => Results.Ok(await sender.Send(new GetSessionBillQuery(id), ct))));

        // Aggregate read: dashboard + floor + ready + requests in one round-trip (cuts 4 polls to 1).
        group.MapGet("/console", (ISender sender, CancellationToken ct) =>
            Exec(async () =>
            {
                var dashboard = await sender.Send(new GetWaiterDashboardQuery(), ct);
                var floor = await sender.Send(new GetTableOverviewQuery(), ct);
                var ready = await sender.Send(new GetReadyToServeQuery(), ct);
                var requests = await sender.Send(new GetCustomerRequestsQuery(CustomerRequestStatus.Pending), ct);
                return Results.Ok(new { dashboard, floor, ready, requests });
            }));

        // ---------- session actions ----------
        group.MapPost("/sessions/open", (OpenSessionRequest body, ISender sender, CancellationToken ct) =>
            Exec(async () =>
            {
                var result = await sender.Send(new OpenSessionCommand(body.TableId, body.GuestCount), ct);
                return Results.Ok(result);
            }));

        group.MapPost("/sessions/{id:guid}/guests", (Guid id, GuestsRequest body, ISender sender, CancellationToken ct) =>
            Exec(async () =>
            {
                await sender.Send(new ChangeSessionGuestCountCommand(id, body.GuestCount), ct);
                return Results.NoContent();
            }));

        group.MapPost("/sessions/{id:guid}/move", (Guid id, MoveRequest body, ISender sender, CancellationToken ct) =>
            Exec(async () =>
            {
                await sender.Send(new MoveSessionTableCommand(id, body.TargetTableId), ct);
                return Results.NoContent();
            }));

        group.MapPost("/sessions/{id:guid}/merge", (Guid id, MergeRequest body, ISender sender, CancellationToken ct) =>
            Exec(async () =>
            {
                await sender.Send(new MergeSessionsCommand(id, body.SourceSessionIds ?? new List<Guid>()), ct);
                return Results.NoContent();
            }));

        group.MapPost("/sessions/{id:guid}/split", (Guid id, SplitRequest body, ISender sender, CancellationToken ct) =>
            Exec(async () =>
            {
                var result = await sender.Send(new SplitSessionCommand(
                    id, body.OrderIds ?? new List<Guid>(), body.TargetTableId, body.GuestCount), ct);
                return Results.Ok(result);
            }));

        group.MapPost("/sessions/{id:guid}/transfer-waiter", (Guid id, TransferWaiterRequest body, ISender sender, CancellationToken ct) =>
            Exec(async () =>
            {
                await sender.Send(new TransferSessionWaiterCommand(id, body.WaiterUserId, body.WaiterName), ct);
                return Results.NoContent();
            }));

        group.MapPost("/sessions/{id:guid}/request-payment", (Guid id, ISender sender, CancellationToken ct) =>
            Exec(async () =>
            {
                await sender.Send(new RequestCashierSettlementCommand(id), ct);
                return Results.NoContent();
            }));

        group.MapPost("/sessions/{id:guid}/close", (Guid id, CloseRequest? body, ISender sender, CancellationToken ct) =>
            Exec(async () =>
            {
                await sender.Send(new CloseSessionCommand(id, body?.Reason), ct);
                return Results.NoContent();
            }))
            .RequireAuthorization("CanCloseSession");

        // ---------- orders / take-order ----------
        group.MapGet("/orders/active", (ISender sender, CancellationToken ct) =>
            Exec(async () => Results.Ok(await sender.Send(new GetActiveOrdersQuery(), ct))));

        group.MapGet("/orders/{id:guid}", (Guid id, ISender sender, CancellationToken ct) =>
            Exec(async () => Results.Ok(await sender.Send(new GetOrderQuery(id), ct))));

        group.MapPost("/orders", (PlaceWaiterOrderRequest body, ISender sender, CancellationToken ct) =>
            Exec(async () =>
            {
                var result = await sender.Send(new PlaceWaiterOrderCommand(
                    body.CustomerPhone, body.CustomerName, body.TableId, body.Type, body.Notes,
                    ToLines(body.Lines), body.GuestCount, body.DiningSessionId), ct);
                return Results.Created($"/waiter/orders/{result.OrderId}", result);
            }));

        group.MapPut("/orders/{id:guid}/lines", (Guid id, UpdateLinesRequest body, ISender sender, CancellationToken ct) =>
            Exec(async () =>
            {
                var result = await sender.Send(new UpdateWaiterOrderLinesCommand(id, ToLines(body.Lines)), ct);
                return Results.Ok(result);
            }));

        group.MapPost("/orders/{id:guid}/status", (Guid id, ChangeStatusRequest body, ISender sender, CancellationToken ct) =>
            Exec(async () =>
            {
                await sender.Send(new ChangeOrderStatusCommand(id, body.Target, body.CancellationReason), ct);
                return Results.NoContent();
            }));

        // ---------- customer requests ----------
        group.MapPost("/requests/{id:guid}/resolve", (Guid id, ISender sender, CancellationToken ct) =>
            Exec(async () =>
            {
                await sender.Send(new ResolveCustomerRequestCommand(id), ct);
                return Results.NoContent();
            }));

        // ---------- catalog / floor reference (client-cached) ----------
        group.MapGet("/catalog/products", (ISender sender, CancellationToken ct) =>
            Exec(async () => Results.Ok(await sender.Send(new GetProductsQuery(), ct))));

        group.MapGet("/catalog/categories", (ISender sender, CancellationToken ct) =>
            Exec(async () => Results.Ok(await sender.Send(new GetProductCategoriesQuery(), ct))));

        group.MapGet("/catalog/tables", (ISender sender, CancellationToken ct) =>
            Exec(async () => Results.Ok(await sender.Send(new GetTablesQuery(), ct))));

        group.MapGet("/catalog/availability", (ISender sender, CancellationToken ct) =>
            Exec(async () => Results.Ok(await sender.Send(new GetProductAvailabilityQuery(), ct))));

        group.MapGet("/staff", (ISender sender, CancellationToken ct) =>
            Exec(async () => Results.Ok(await sender.Send(new GetUsersQuery(), ct))));

        // ---------- KOT / bill PDFs (download via authenticated dio, then open) ----------
        group.MapGet("/orders/{id:guid}/kot.pdf", (
            Guid id, ClaimsPrincipal user, ISender sender, IReportRenderer renderer,
            IOptions<ReceiptBranding> branding, CancellationToken ct) =>
            Exec(async () =>
            {
                var order = await sender.Send(new GetOrderQuery(id), ct);
                var pdf = await renderer.RenderKitchenTicketAsync(ToKitchenTicketData(order, branding.Value, user), ct);
                return Results.File(pdf, "application/pdf", $"{order.OrderNumber}-kot.pdf");
            }));

        group.MapGet("/orders/{id:guid}/bill.pdf", (
            Guid id, ClaimsPrincipal user, ISender sender, IReportRenderer renderer,
            IOptions<ReceiptBranding> branding, CancellationToken ct) =>
            Exec(async () =>
            {
                var order = await sender.Send(new GetOrderQuery(id), ct);
                var pdf = await renderer.RenderOrderReceiptAsync(ToReceiptData(order, branding.Value, user), ct);
                return Results.File(pdf, "application/pdf", $"{order.OrderNumber}.pdf");
            }));

        return app;
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

    private static List<PlaceOrderLineInput> ToLines(IEnumerable<WaiterLineRequest>? lines) =>
        (lines ?? Enumerable.Empty<WaiterLineRequest>())
            .Select(l => new PlaceOrderLineInput(l.MenuItemId, l.Quantity, l.VariantId, l.Notes))
            .ToList();

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

    // ---------- request bodies ----------
    public record OpenSessionRequest(Guid TableId, int GuestCount = 0);
    public record GuestsRequest(int GuestCount);
    public record MoveRequest(Guid TargetTableId);
    public record MergeRequest(List<Guid> SourceSessionIds);
    public record SplitRequest(List<Guid> OrderIds, Guid TargetTableId, int GuestCount = 0);
    public record TransferWaiterRequest(Guid? WaiterUserId, string? WaiterName);
    public record CloseRequest(string? Reason);
    public record WaiterLineRequest(Guid MenuItemId, int Quantity, Guid? VariantId = null, string? Notes = null);
    public record PlaceWaiterOrderRequest(
        string? CustomerPhone, string? CustomerName, Guid? TableId, OrderType Type, string? Notes,
        List<WaiterLineRequest> Lines, int? GuestCount = null, Guid? DiningSessionId = null);
    public record UpdateLinesRequest(List<WaiterLineRequest> Lines);
    public record ChangeStatusRequest(OrderStatus Target, string? CancellationReason = null);
}
