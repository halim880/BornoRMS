using BornoBit.Restaurant.Application.Settings;
using BornoBit.Restaurant.Shared.Common;
using FluentValidation;
using MediatR;

namespace BornoBit.Restaurant.Api.Endpoints;

/// <summary>
/// REST surface for the Flutter App Settings screen — mirrors the Blazor settings page
/// (AppSettings.razor) and the billing defaults handler (BillingSettingsCommands.cs).
/// GET is Staff-readable; the update is Admin-only (matching the handler's PermissionGuard).
/// Every route forwards to an existing Application-layer MediatR handler (registered via AddApplication()).
/// Mounted under the versioned group → /api/v1/staff/settings/*.
/// </summary>
public static class SettingsEndpoints
{
    public static IEndpointRouteBuilder MapSettingsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/staff/settings")
            .RequireCors("Frontends")
            .WithTags("Settings");

        // ---------- read (any staff) ----------
        group.MapGet("/", (ISender sender, CancellationToken ct) =>
            Exec(async () => Results.Ok(await sender.Send(new GetBillingSettingsQuery(), ct))))
            .RequireAuthorization("Staff");

        // ---------- update (admin only) ----------
        group.MapPut("/", (UpdateSettingsRequest body, ISender sender, CancellationToken ct) =>
            Exec(async () =>
            {
                var result = await sender.Send(new UpdateBillingSettingsCommand(
                    body.VatPercent, body.ServiceChargePercent, body.Currency, body.TipEnabled,
                    body.HighDiscountThresholdPercent, body.PriceIncludesTax), ct);
                return Results.Ok(result);
            }))
            .RequireAuthorization("Admin");

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
    public record UpdateSettingsRequest(
        decimal VatPercent,
        decimal ServiceChargePercent,
        string Currency,
        bool TipEnabled,
        decimal HighDiscountThresholdPercent,
        bool PriceIncludesTax);
}
