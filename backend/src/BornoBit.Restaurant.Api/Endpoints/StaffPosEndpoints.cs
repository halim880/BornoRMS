using BornoBit.Restaurant.Api.Services;
using BornoBit.Restaurant.Application.Accounting.Drawers;
using BornoBit.Restaurant.Application.Dining.Queries;
using BornoBit.Restaurant.Application.Ordering.Commands;
using BornoBit.Restaurant.Application.Ordering.Pos;
using BornoBit.Restaurant.Application.Ordering.Queries;
using BornoBit.Restaurant.Application.ProductCategories;
using BornoBit.Restaurant.Application.Products;
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
/// REST surface for the Flutter POS terminal — mirrors the Blazor POS page (Pos.razor).
/// Every route forwards to an existing Application-layer handler (registered via AddApplication()).
/// Mounted under the versioned group → /api/v1/staff/pos/*. Polling, no SignalR.
/// </summary>
public static class StaffPosEndpoints
{
    public static IEndpointRouteBuilder MapStaffPosEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/staff/pos")
            .RequireCors("Frontends")
            .RequireAuthorization("Staff")
            .WithTags("StaffPos");

        // ---------- catalog ----------
        group.MapGet("/catalog/products", (ISender sender, CancellationToken ct) =>
            Exec(async () => Results.Ok(await sender.Send(new GetProductsQuery(), ct))));

        group.MapGet("/catalog/categories", (ISender sender, CancellationToken ct) =>
            Exec(async () => Results.Ok(await sender.Send(new GetProductCategoriesQuery(), ct))));

        group.MapGet("/catalog/availability", (ISender sender, CancellationToken ct) =>
            Exec(async () => Results.Ok(await sender.Send(new GetProductAvailabilityQuery(), ct))));

        group.MapGet("/catalog/products/{id:guid}/option-groups", (Guid id, ISender sender, CancellationToken ct) =>
            Exec(async () => Results.Ok(await sender.Send(new GetProductOptionGroupsQuery(id), ct))));

        group.MapGet("/tables", (ISender sender, CancellationToken ct) =>
            Exec(async () => Results.Ok(await sender.Send(new GetTablesQuery(), ct))));

        // ---------- active orders ----------
        group.MapGet("/orders/active", (ISender sender, CancellationToken ct) =>
            Exec(async () => Results.Ok(await sender.Send(new GetActiveOrdersQuery(), ct))));

        // ---------- order lifecycle ----------
        group.MapPost("/orders", (CreatePosOrderRequest body, ISender sender, ILiveNotifier live, CancellationToken ct) =>
            Exec(async () =>
            {
                var result = await sender.Send(new CreatePosOrderCommand(
                    body.Type, body.TableId, body.CustomerPhone, body.CustomerName, body.CustomerAddress,
                    body.DeliveryCharge ?? 0m), ct);
                await live.NotifyAsync(LiveScopes.Orders, ct);
                if (body.Type == OrderType.Delivery) await live.NotifyAsync(LiveScopes.Delivery, ct);
                return Results.Created($"/api/v1/staff/orders/{result.OrderId}", result);
            }));

        group.MapPatch("/orders/{id:guid}", (Guid id, UpdatePosOrderRequest body, ISender sender, ILiveNotifier live, CancellationToken ct) =>
            Exec(async () =>
            {
                var result = await sender.Send(new UpdatePosOrderCommand(
                    id, body.Type, body.TableId, body.CustomerPhone, body.CustomerName, body.CustomerAddress), ct);
                await live.NotifyAsync(LiveScopes.Orders, ct);
                return Results.Ok(result);
            }));

        group.MapPut("/orders/{id:guid}/lines", (Guid id, SetLinesRequest body, ISender sender, ILiveNotifier live, CancellationToken ct) =>
            Exec(async () =>
            {
                var result = await sender.Send(new SetPosOrderLinesCommand(id, ToLines(body.Lines)), ct);
                await live.NotifyAsync(LiveScopes.Orders, ct);
                return Results.Ok(result);
            }));

        // ---------- billing ----------
        group.MapPost("/orders/{id:guid}/discount", (Guid id, DiscountRequest body, ISender sender, ILiveNotifier live, CancellationToken ct) =>
            Exec(async () =>
            {
                var result = await sender.Send(new ApplyDiscountCommand(id, body.Percent, body.Amount, body.Reason), ct);
                await live.NotifyAsync(LiveScopes.Orders, ct);
                return Results.Ok(result);
            }));

        group.MapPost("/orders/{id:guid}/rounding", (Guid id, RoundingRequest body, ISender sender, ILiveNotifier live, CancellationToken ct) =>
            Exec(async () =>
            {
                var result = await sender.Send(new ApplyPosRoundingCommand(id, body.Mode), ct);
                await live.NotifyAsync(LiveScopes.Orders, ct);
                return Results.Ok(result);
            }));

        group.MapPost("/orders/{id:guid}/payments", (Guid id, PaymentsRequest body, ISender sender, ILiveNotifier live, CancellationToken ct) =>
            Exec(async () =>
            {
                var result = await sender.Send(new AddPaymentCommand(id, ToPayments(body.Payments), body.IdempotencyKey), ct);
                await live.NotifyAsync(LiveScopes.Payments, ct);
                return Results.Ok(result);
            }));

        // Void a mistaken captured payment / refund part or all of one. Manager-gated in the handler
        // (a manager role on the till proceeds; otherwise managerUserName/Password authorizes the override).
        group.MapPost("/orders/{id:guid}/payments/{paymentId:guid}/void", (Guid id, Guid paymentId, VoidPaymentRequest body, ISender sender, ILiveNotifier live, CancellationToken ct) =>
            Exec(async () =>
            {
                var result = await sender.Send(new VoidPaymentCommand(id, paymentId, body.Reason, body.ManagerUserName, body.ManagerPassword), ct);
                await live.NotifyAsync(LiveScopes.Payments, ct);
                return Results.Ok(result);
            }));

        group.MapPost("/orders/{id:guid}/payments/{paymentId:guid}/refund", (Guid id, Guid paymentId, RefundPaymentRequest body, ISender sender, ILiveNotifier live, CancellationToken ct) =>
            Exec(async () =>
            {
                var result = await sender.Send(new RefundPaymentCommand(id, paymentId, body.Amount, body.Reason, body.ManagerUserName, body.ManagerPassword), ct);
                await live.NotifyAsync(LiveScopes.Payments, ct);
                return Results.Ok(result);
            }));

        group.MapPost("/orders/{id:guid}/cancel", (Guid id, CancelRequest? body, ISender sender, ILiveNotifier live, CancellationToken ct) =>
            Exec(async () =>
            {
                await sender.Send(new ChangeOrderStatusCommand(id, OrderStatus.Cancelled, body?.Reason), ct);
                await live.NotifyAsync(LiveScopes.Orders, ct);
                return Results.NoContent();
            }));

        // ---------- cash drawer / shift (cashier-accessible; handlers enforce role) ----------
        // The cashier's open drawer (null if none) — POS checks this before allowing a cash tender.
        group.MapGet("/drawers/current", (ISender sender, CancellationToken ct) =>
            Exec(async () => Results.Ok(await sender.Send(new GetCurrentDrawerQuery(), ct))));

        // Takings broken down by payment method, for the end-of-shift close screen.
        group.MapGet("/drawers/{id:guid}/summary", (Guid id, ISender sender, CancellationToken ct) =>
            Exec(async () => Results.Ok(await sender.Send(new GetDrawerSummaryQuery(id), ct))));

        group.MapPost("/drawers/open", (OpenDrawerRequest body, ISender sender, ILiveNotifier live, CancellationToken ct) =>
            Exec(async () =>
            {
                var result = await sender.Send(new OpenDrawerCommand(body.OpeningBalance, body.CashAccountId, body.Notes), ct);
                await live.NotifyAsync(LiveScopes.Payments, ct);
                return Results.Created($"/api/v1/staff/pos/drawers/{result.Id}", result);
            }));

        group.MapPost("/drawers/{id:guid}/close", (Guid id, CloseDrawerRequest body, ISender sender, ILiveNotifier live, CancellationToken ct) =>
            Exec(async () =>
            {
                var result = await sender.Send(new CloseDrawerCommand(id, body.CountedBalance, body.Notes), ct);
                await live.NotifyAsync(LiveScopes.Payments, ct);
                return Results.Ok(result);
            }));

        // ---------- KOT PDF (Staff-auth fallback when no thermal printer; receipt reuses /admin/orders/{id}/pos-receipt.pdf) ----------
        group.MapGet("/orders/{id:guid}/kot.pdf", (
            Guid id, ClaimsPrincipal user, ISender sender, IReportRenderer renderer,
            IOptions<ReceiptBranding> branding, CancellationToken ct) =>
            Exec(async () =>
            {
                var order = await sender.Send(new GetOrderQuery(id), ct);
                var pdf = await renderer.RenderKitchenTicketAsync(ToKitchenTicketData(order, branding.Value, user), ct);
                return Results.File(pdf, "application/pdf", $"{order.OrderNumber}-kot.pdf");
            }));

        return app;
    }

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

    private static List<PlaceOrderLineInput> ToLines(IEnumerable<PosLineRequest>? lines) =>
        (lines ?? Enumerable.Empty<PosLineRequest>())
            .Select(l => new PlaceOrderLineInput(l.MenuItemId, l.Quantity, l.VariantId, l.Notes, l.OptionIds))
            .ToList();

    private static List<PaymentEntryInput> ToPayments(IEnumerable<PaymentEntryRequest>? payments) =>
        (payments ?? Enumerable.Empty<PaymentEntryRequest>())
            .Select(p => new PaymentEntryInput(p.Method, p.Provider, p.Amount, p.Tendered, p.Reference))
            .ToList();

    // ---------- request bodies ----------
    public record CreatePosOrderRequest(OrderType Type, Guid? TableId, string? CustomerPhone, string? CustomerName, string? CustomerAddress, decimal? DeliveryCharge = null);
    public record UpdatePosOrderRequest(OrderType Type, Guid? TableId, string? CustomerPhone, string? CustomerName, string? CustomerAddress);
    public record PosLineRequest(Guid MenuItemId, int Quantity, Guid? VariantId = null, string? Notes = null, List<Guid>? OptionIds = null);
    public record SetLinesRequest(List<PosLineRequest> Lines);
    public record DiscountRequest(decimal? Percent, decimal? Amount, string? Reason);
    public record RoundingRequest(PosRoundingMode Mode);
    public record PaymentEntryRequest(PaymentMethod Method, PaymentProvider? Provider, decimal Amount, decimal Tendered, string? Reference = null);
    public record PaymentsRequest(List<PaymentEntryRequest> Payments, string? IdempotencyKey = null);
    public record CancelRequest(string? Reason);
    public record VoidPaymentRequest(string Reason, string? ManagerUserName = null, string? ManagerPassword = null);
    public record RefundPaymentRequest(decimal Amount, string Reason, string? ManagerUserName = null, string? ManagerPassword = null);
    public record OpenDrawerRequest(decimal OpeningBalance, Guid? CashAccountId = null, string? Notes = null);
    public record CloseDrawerRequest(decimal CountedBalance, string? Notes = null);
}
