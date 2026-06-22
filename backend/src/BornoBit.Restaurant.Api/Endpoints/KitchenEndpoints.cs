using BornoBit.Restaurant.Api.Services;
using BornoBit.Restaurant.Application.Kitchen.Commands;
using BornoBit.Restaurant.Application.Kitchen.Queries;
using BornoBit.Restaurant.Application.Operations.Dashboard;
using BornoBit.Restaurant.Application.Ordering.Commands;
using BornoBit.Restaurant.Domain.Ordering;
using BornoBit.Restaurant.Shared.Common;
using FluentValidation;
using MediatR;

namespace BornoBit.Restaurant.Api.Endpoints;

/// <summary>
/// REST surface for the Flutter Kitchen Display (KDS) — mirrors the Blazor Kitchen Display page
/// (KitchenDisplay.razor). Every route forwards to an existing Application-layer MediatR handler
/// (registered via AddApplication()). Mounted under the versioned group → /api/v1/staff/kitchen/*.
/// Mobile/desktop uses polling, so there is no SignalR here.
/// </summary>
public static class KitchenEndpoints
{
    public static IEndpointRouteBuilder MapKitchenEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/staff/kitchen")
            .RequireCors("Frontends")
            .RequireAuthorization("Kitchen")
            .WithTags("Kitchen");

        // ---------- live board (polled) ----------
        // The kitchen board grouped into Pending / Preparing / Ready columns, with optional
        // station / type / table / order-number filters (mirrors the Blazor filters).
        group.MapGet("/board", (
            ISender sender,
            Guid? kitchenId, Guid? stationId, OrderType? type, OrderStatus? status,
            string? tableNumber, string? search, DateOnly? date,
            CancellationToken ct) =>
            Exec(async () => Results.Ok(await sender.Send(new GetKitchenBoardQuery(
                KitchenId: kitchenId,
                StationId: stationId,
                Type: type,
                Status: status,
                TableNumber: string.IsNullOrWhiteSpace(tableNumber) ? null : tableNumber,
                SearchOrderNumber: string.IsNullOrWhiteSpace(search) ? null : search,
                Date: date), ct))));

        group.MapGet("/stations", (ISender sender, bool? includeInactive, CancellationToken ct) =>
            Exec(async () => Results.Ok(await sender.Send(
                new GetKitchenStationsQuery(includeInactive ?? false), ct))));

        // ---------- kitchens (physical kitchens grouping stations) ----------
        group.MapGet("/kitchens", (ISender sender, bool? includeInactive, CancellationToken ct) =>
            Exec(async () => Results.Ok(await sender.Send(
                new GetKitchensQuery(includeInactive ?? false), ct))));

        group.MapPost("/kitchens", (CreateKitchenRequest body, ISender sender, CancellationToken ct) =>
            Exec(async () =>
            {
                var id = await sender.Send(new CreateKitchenCommand(
                    body.Name, body.Code, body.ColorHex, body.PrinterName, body.DisplayOrder), ct);
                return Results.Ok(new { id });
            }));

        group.MapPut("/kitchens/{id:guid}", (Guid id, UpdateKitchenRequest body, ISender sender, CancellationToken ct) =>
            Exec(async () =>
            {
                await sender.Send(new UpdateKitchenCommand(
                    id, body.Name, body.Code, body.ColorHex, body.PrinterName, body.DisplayOrder), ct);
                return Results.NoContent();
            }));

        group.MapPost("/kitchens/{id:guid}/active", (Guid id, ToggleActiveRequest body, ISender sender, CancellationToken ct) =>
            Exec(async () =>
            {
                await sender.Send(new ToggleKitchenActiveCommand(id, body.IsActive), ct);
                return Results.NoContent();
            }));

        // Route a station to a kitchen (null clears → default kitchen).
        group.MapPost("/stations/{id:guid}/kitchen", (Guid id, AssignStationKitchenRequest body, ISender sender, CancellationToken ct) =>
            Exec(async () =>
            {
                await sender.Send(new AssignStationKitchenCommand(id, body.KitchenId), ct);
                return Results.NoContent();
            }));

        group.MapGet("/metrics", (ISender sender, CancellationToken ct) =>
            Exec(async () => Results.Ok(await sender.Send(new GetKitchenPerformanceQuery(), ct))));

        group.MapGet("/analytics", (ISender sender, DateOnly? from, DateOnly? to, int? topN, CancellationToken ct) =>
            Exec(async () =>
            {
                var today = DateOnly.FromDateTime(DateTime.UtcNow);
                return Results.Ok(await sender.Send(new GetKitchenAnalyticsQuery(
                    from ?? today, to ?? today, topN ?? 5), ct));
            }));

        // Aggregate read: board + stations + metrics in one round-trip (cuts 3 polls to 1).
        group.MapGet("/console", (
            ISender sender,
            Guid? kitchenId, Guid? stationId, OrderType? type, OrderStatus? status,
            string? tableNumber, string? search, DateOnly? date,
            CancellationToken ct) =>
            Exec(async () =>
            {
                var board = await sender.Send(new GetKitchenBoardQuery(
                    KitchenId: kitchenId,
                    StationId: stationId,
                    Type: type,
                    Status: status,
                    TableNumber: string.IsNullOrWhiteSpace(tableNumber) ? null : tableNumber,
                    SearchOrderNumber: string.IsNullOrWhiteSpace(search) ? null : search,
                    Date: date), ct);
                var stations = await sender.Send(new GetKitchenStationsQuery(), ct);
                var kitchens = await sender.Send(new GetKitchensQuery(), ct);
                var metrics = await sender.Send(new GetKitchenPerformanceQuery(), ct);
                return Results.Ok(new { board, stations, kitchens, metrics });
            }));

        // ---------- order actions ----------
        // Accept a still-Placed order: confirms it and fires the kitchen ticket.
        group.MapPost("/orders/{id:guid}/accept", (Guid id, ISender sender, ILiveNotifier live, CancellationToken ct) =>
            Exec(async () =>
            {
                var newStatus = await sender.Send(new AcceptKitchenOrderCommand(id), ct);
                await live.NotifyAsync(LiveScopes.Kitchen, ct);
                return Results.Ok(new { status = newStatus.ToString() });
            }));

        // Single-click advance through the fulfilment track (Confirmed → Preparing → Ready → Served).
        group.MapPost("/orders/{id:guid}/advance", (Guid id, ISender sender, ILiveNotifier live, CancellationToken ct) =>
            Exec(async () =>
            {
                var newStatus = await sender.Send(new AdvanceKitchenOrderCommand(id), ct);
                await live.NotifyAsync(LiveScopes.Kitchen, ct);
                return Results.Ok(new { status = newStatus.ToString() });
            }));

        // Explicit status change (e.g. bump Preparing → Ready, or cancel) via the shared order command.
        group.MapPost("/orders/{id:guid}/status", (Guid id, ChangeStatusRequest body, ISender sender, ILiveNotifier live, CancellationToken ct) =>
            Exec(async () =>
            {
                await sender.Send(new ChangeOrderStatusCommand(id, body.Target, body.CancellationReason), ct);
                await live.NotifyAsync(LiveScopes.Kitchen, ct);
                return Results.NoContent();
            }));

        group.MapPost("/orders/{id:guid}/priority", (Guid id, PriorityRequest body, ISender sender, ILiveNotifier live, CancellationToken ct) =>
            Exec(async () =>
            {
                await sender.Send(new ToggleOrderPriorityCommand(id, body.IsPriority), ct);
                await live.NotifyAsync(LiveScopes.Kitchen, ct);
                return Results.NoContent();
            }));

        group.MapPost("/orders/{id:guid}/notes", (Guid id, NotesRequest body, ISender sender, ILiveNotifier live, CancellationToken ct) =>
            Exec(async () =>
            {
                await sender.Send(new UpdateKitchenNotesCommand(id, body.Notes), ct);
                await live.NotifyAsync(LiveScopes.Kitchen, ct);
                return Results.NoContent();
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

    // ---------- request bodies ----------
    public record ChangeStatusRequest(OrderStatus Target, string? CancellationReason = null);
    public record PriorityRequest(bool IsPriority);
    public record NotesRequest(string? Notes);
    public record CreateKitchenRequest(string Name, string? Code, string? ColorHex, string? PrinterName, int DisplayOrder);
    public record UpdateKitchenRequest(string Name, string? Code, string? ColorHex, string? PrinterName, int DisplayOrder);
    public record ToggleActiveRequest(bool IsActive);
    public record AssignStationKitchenRequest(Guid? KitchenId);
}
