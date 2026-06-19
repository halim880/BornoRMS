using BornoBit.Restaurant.Api.Services;
using BornoBit.Restaurant.Application.Logistics;
using BornoBit.Restaurant.Shared.Common;
using FluentValidation;
using MediatR;

namespace BornoBit.Restaurant.Api.Endpoints;

/// <summary>
/// REST surface for the Flutter Delivery module — dispatch board, rider roster, and the
/// delivery lifecycle (assign → out-for-delivery → delivered/failed). Mounted under
/// /api/v1/staff/delivery/*. COD itself is settled by the cashier via the POS payment flow;
/// these routes only move the dispatch state and surface the COD-expected balances.
/// </summary>
public static class LogisticsEndpoints
{
    public static IEndpointRouteBuilder MapLogisticsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/staff/delivery")
            .RequireCors("Frontends")
            .RequireAuthorization("Delivery")
            .WithTags("Delivery");

        // ---------- board + reconciliation ----------
        group.MapGet("/board", (ISender sender, DateTime? date, bool? unpaidOnly, int? page, int? pageSize, CancellationToken ct) =>
            Exec(async () => Results.Ok(await sender.Send(new GetDeliveryBoardQuery(
                date is { } d ? DateOnly.FromDateTime(d) : null,
                unpaidOnly ?? false,
                page is > 0 ? page.Value : 1,
                pageSize is > 0 ? pageSize.Value : 50), ct))));

        group.MapGet("/cod-reconciliation", (ISender sender, DateTime? date, CancellationToken ct) =>
            Exec(async () => Results.Ok(await sender.Send(new GetRiderCodReconciliationQuery(
                date is { } d ? DateOnly.FromDateTime(d) : null), ct))));

        // ---------- delivery lifecycle ----------
        group.MapPost("/{orderId:guid}/assign", (Guid orderId, AssignRiderRequest body, ISender sender, ILiveNotifier live, CancellationToken ct) =>
            Exec(async () =>
            {
                await sender.Send(new AssignRiderCommand(orderId, body.RiderId), ct);
                await live.NotifyAsync(LiveScopes.Delivery, ct);
                return Results.NoContent();
            }));

        group.MapPost("/{orderId:guid}/out-for-delivery", (Guid orderId, ISender sender, ILiveNotifier live, CancellationToken ct) =>
            Exec(async () =>
            {
                await sender.Send(new MarkOutForDeliveryCommand(orderId), ct);
                await live.NotifyAsync(LiveScopes.Delivery, ct);
                await live.NotifyAsync(LiveScopes.Kitchen, ct);
                return Results.NoContent();
            }));

        group.MapPost("/{orderId:guid}/delivered", (Guid orderId, ISender sender, ILiveNotifier live, CancellationToken ct) =>
            Exec(async () =>
            {
                await sender.Send(new MarkDeliveredCommand(orderId), ct);
                await live.NotifyAsync(LiveScopes.Delivery, ct);
                await live.NotifyAsync(LiveScopes.Orders, ct);
                // A delivered COD order now shows as cash-expected on the cash-counter board.
                await live.NotifyAsync(LiveScopes.Payments, ct);
                return Results.NoContent();
            }));

        group.MapPost("/{orderId:guid}/failed", (Guid orderId, FailDeliveryRequest body, ISender sender, ILiveNotifier live, CancellationToken ct) =>
            Exec(async () =>
            {
                await sender.Send(new MarkDeliveryFailedCommand(orderId, body.Reason), ct);
                await live.NotifyAsync(LiveScopes.Delivery, ct);
                return Results.NoContent();
            }));

        group.MapPost("/{orderId:guid}/cancel", (Guid orderId, CancelDeliveryRequest body, ISender sender, ILiveNotifier live, CancellationToken ct) =>
            Exec(async () =>
            {
                await sender.Send(new CancelDeliveryCommand(orderId, body.Reason), ct);
                await live.NotifyAsync(LiveScopes.Delivery, ct);
                await live.NotifyAsync(LiveScopes.Orders, ct);
                return Results.NoContent();
            }));

        // ---------- rider roster ----------
        group.MapGet("/riders", (ISender sender, bool? includeInactive, CancellationToken ct) =>
            Exec(async () => Results.Ok(await sender.Send(new GetRidersQuery(includeInactive ?? false), ct))));

        group.MapPost("/riders", (CreateRiderRequest body, ISender sender, CancellationToken ct) =>
            Exec(async () =>
            {
                var id = await sender.Send(new CreateRiderCommand(body.Name, body.Phone, body.Vehicle), ct);
                return Results.Created($"/api/v1/staff/delivery/riders/{id}", new { id });
            }));

        group.MapPut("/riders/{id:guid}", (Guid id, UpdateRiderRequest body, ISender sender, CancellationToken ct) =>
            Exec(async () =>
            {
                await sender.Send(new UpdateRiderCommand(id, body.Name, body.Phone, body.Vehicle), ct);
                return Results.NoContent();
            }));

        group.MapPost("/riders/{id:guid}/active", (Guid id, SetRiderActiveRequest body, ISender sender, CancellationToken ct) =>
            Exec(async () =>
            {
                await sender.Send(new SetRiderActiveCommand(id, body.Active), ct);
                return Results.NoContent();
            }));

        return app;
    }

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
        catch (InvalidOperationException ex)
        {
            return Results.Conflict(new { message = ex.Message });
        }
    }

    public record AssignRiderRequest(Guid RiderId);
    public record FailDeliveryRequest(string? Reason);
    public record CancelDeliveryRequest(string? Reason);
    public record CreateRiderRequest(string Name, string Phone, string? Vehicle);
    public record UpdateRiderRequest(string Name, string Phone, string? Vehicle);
    public record SetRiderActiveRequest(bool Active);
}
