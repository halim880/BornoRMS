using BornoBit.Restaurant.Application.MenuPermissions;
using BornoBit.Restaurant.Application.Modules;
using BornoBit.Restaurant.Application.Numbering;
using BornoBit.Restaurant.Application.RoleManagement;
using BornoBit.Restaurant.Application.Tenants;
using BornoBit.Restaurant.Application.Users;
using BornoBit.Restaurant.Domain.Numbering;
using BornoBit.Restaurant.Shared.Common;
using FluentValidation;
using MediatR;

namespace BornoBit.Restaurant.Api.Endpoints;

/// <summary>
/// REST surface for the Flutter Admin screens — mirrors the Blazor admin pages
/// (Users.razor, Roles.razor, MenuPermissions.razor, ModulePermissions.razor,
/// NumberingScopes.razor, Tenants.razor, Modules.razor). Every route forwards to an existing
/// MediatR handler. The API registers BOTH Application and Infrastructure handlers, so the
/// Users/Roles handlers (which live in Infrastructure/Identity) are reachable here.
/// Mounted under the versioned group → /api/v1/staff/admin/*. Admin-only; the SuperAdmin-scoped
/// areas (tenants, modules, numbering-scopes writes) require the SuperAdmin policy to mirror Blazor.
/// </summary>
public static class AdminEndpoints
{
    public static IEndpointRouteBuilder MapAdminEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/staff/admin")
            .RequireCors("Frontends")
            .RequireAuthorization("Admin")
            .WithTags("Admin");

        // ---------- users ----------
        group.MapGet("/users", (bool? includeInactive, ISender sender, CancellationToken ct) =>
            Exec(async () => Results.Ok(await sender.Send(new GetUsersQuery(includeInactive ?? false), ct))));

        group.MapPost("/users", (CreateUserRequest body, ISender sender, CancellationToken ct) =>
            Exec(async () =>
            {
                var result = await sender.Send(new CreateUserCommand(
                    body.UserName, body.Email, body.FullName,
                    body.Roles ?? new List<string>(), body.InitialPassword), ct);
                return Results.Created($"/api/v1/staff/admin/users/{result.UserId}", result);
            }));

        group.MapPatch("/users/{id:guid}", (Guid id, UpdateUserRequest body, ISender sender, CancellationToken ct) =>
            Exec(async () =>
            {
                await sender.Send(new UpdateUserCommand(
                    id, body.UserName, body.Email, body.FullName, body.Roles ?? new List<string>()), ct);
                return Results.NoContent();
            }));

        group.MapPost("/users/{id:guid}/reset-password", (Guid id, ISender sender, CancellationToken ct) =>
            Exec(async () =>
            {
                var password = await sender.Send(new ResetPasswordCommand(id), ct);
                return Results.Ok(new { password });
            }));

        group.MapPost("/users/{id:guid}/active", (Guid id, SetActiveRequest body, ISender sender, CancellationToken ct) =>
            Exec(async () =>
            {
                await sender.Send(new SetUserActiveCommand(id, body.IsActive), ct);
                return Results.NoContent();
            }));

        // ---------- roles ----------
        group.MapGet("/roles", (ISender sender, CancellationToken ct) =>
            Exec(async () => Results.Ok(await sender.Send(new GetRoleListQuery(), ct))));

        group.MapPost("/roles", (CreateRoleRequest body, ISender sender, CancellationToken ct) =>
            Exec(async () =>
            {
                var id = await sender.Send(new CreateRoleCommand(body.Name, body.Description), ct);
                return Results.Created($"/api/v1/staff/admin/roles/{id}", new { id });
            }));

        group.MapPatch("/roles/{id:guid}", (Guid id, UpdateRoleRequest body, ISender sender, CancellationToken ct) =>
            Exec(async () =>
            {
                await sender.Send(new UpdateRoleCommand(id, body.Name, body.Description), ct);
                return Results.NoContent();
            }));

        group.MapDelete("/roles/{id:guid}", (Guid id, ISender sender, CancellationToken ct) =>
            Exec(async () =>
            {
                await sender.Send(new DeleteRoleCommand(id), ct);
                return Results.NoContent();
            }));

        // ---------- menu permissions ----------
        group.MapGet("/menu-permissions", (Guid roleId, ISender sender, CancellationToken ct) =>
            Exec(async () => Results.Ok(await sender.Send(new GetMenuPermissionsTreeQuery(roleId), ct))));

        group.MapPost("/menu-permissions", (SaveMenuPermissionsRequest body, ISender sender, CancellationToken ct) =>
            Exec(async () =>
            {
                await sender.Send(new SaveMenuPermissionsCommand(
                    body.RoleId, body.PermittedMenuIds ?? new List<Guid>()), ct);
                return Results.NoContent();
            }));

        // ---------- module permissions ----------
        group.MapGet("/module-permissions", (Guid roleId, ISender sender, CancellationToken ct) =>
            Exec(async () => Results.Ok(await sender.Send(new GetModulePermissionsQuery(roleId), ct))));

        group.MapPost("/module-permissions", (SaveModulePermissionsRequest body, ISender sender, CancellationToken ct) =>
            Exec(async () =>
            {
                await sender.Send(new SaveModulePermissionsCommand(
                    body.RoleId, body.PermittedModuleIds ?? new List<Guid>()), ct);
                return Results.NoContent();
            }));

        // Roles picker source shared by the menu/module permission screens.
        group.MapGet("/permission-roles", (ISender sender, CancellationToken ct) =>
            Exec(async () => Results.Ok(await sender.Send(new GetRolesQuery(), ct))));

        // ---------- numbering scopes (SuperAdmin for writes) ----------
        group.MapGet("/numbering-scopes", (bool? includeInactive, ISender sender, CancellationToken ct) =>
            Exec(async () => Results.Ok(await sender.Send(new GetNumberingScopesQuery(includeInactive ?? true), ct))));

        group.MapPost("/numbering-scopes", (CreateNumberingScopeRequest body, ISender sender, CancellationToken ct) =>
            Exec(async () =>
            {
                var id = await sender.Send(new CreateNumberingScopeCommand(
                    body.Code, body.Name, body.Prefix, body.Cadence, body.Digits, body.ResetByOutlet), ct);
                return Results.Created($"/api/v1/staff/admin/numbering-scopes/{id}", new { id });
            }))
            .RequireAuthorization("SuperAdmin");

        group.MapPatch("/numbering-scopes/{id:guid}", (Guid id, UpdateNumberingScopeRequest body, ISender sender, CancellationToken ct) =>
            Exec(async () =>
            {
                await sender.Send(new UpdateNumberingScopeCommand(
                    id, body.Name, body.Prefix, body.Cadence, body.Digits, body.ResetByOutlet), ct);
                return Results.NoContent();
            }))
            .RequireAuthorization("SuperAdmin");

        group.MapPost("/numbering-scopes/{id:guid}/active", (Guid id, SetActiveRequest body, ISender sender, CancellationToken ct) =>
            Exec(async () =>
            {
                await sender.Send(new SetNumberingScopeActiveCommand(id, body.IsActive), ct);
                return Results.NoContent();
            }))
            .RequireAuthorization("SuperAdmin");

        // ---------- tenants (SuperAdmin for writes) ----------
        group.MapGet("/tenants", (bool? includeInactive, ISender sender, CancellationToken ct) =>
            Exec(async () => Results.Ok(await sender.Send(new GetTenantsQuery(includeInactive ?? false), ct))));

        group.MapPost("/tenants", (CreateTenantRequest body, ISender sender, CancellationToken ct) =>
            Exec(async () =>
            {
                var id = await sender.Send(new CreateTenantCommand(
                    body.Name, body.Subdomain, body.ContactEmail, body.LicenseExpiresOnUtc), ct);
                return Results.Created($"/api/v1/staff/admin/tenants/{id}", new { id });
            }))
            .RequireAuthorization("SuperAdmin");

        group.MapPatch("/tenants/{id:guid}", (Guid id, UpdateTenantRequest body, ISender sender, CancellationToken ct) =>
            Exec(async () =>
            {
                await sender.Send(new UpdateTenantCommand(
                    id, body.Name, body.ContactEmail, body.LicenseExpiresOnUtc), ct);
                return Results.NoContent();
            }))
            .RequireAuthorization("SuperAdmin");

        group.MapPost("/tenants/{id:guid}/active", (Guid id, SetActiveRequest body, ISender sender, CancellationToken ct) =>
            Exec(async () =>
            {
                await sender.Send(new SetTenantActiveCommand(id, body.IsActive), ct);
                return Results.NoContent();
            }))
            .RequireAuthorization("SuperAdmin");

        // ---------- modules (SuperAdmin for writes; handlers self-guard too) ----------
        group.MapGet("/modules", (ISender sender, CancellationToken ct) =>
            Exec(async () => Results.Ok(await sender.Send(new GetAllModulesQuery(), ct))))
            .RequireAuthorization("SuperAdmin");

        group.MapPost("/modules", (CreateModuleRequest body, ISender sender, CancellationToken ct) =>
            Exec(async () =>
            {
                var id = await sender.Send(new CreateModuleCommand(
                    body.Title, body.Icon, body.DisplayOrder, body.RequiredRole), ct);
                return Results.Created($"/api/v1/staff/admin/modules/{id}", new { id });
            }))
            .RequireAuthorization("SuperAdmin");

        group.MapPatch("/modules/{id:guid}", (Guid id, UpdateModuleRequest body, ISender sender, CancellationToken ct) =>
            Exec(async () =>
            {
                await sender.Send(new UpdateModuleCommand(
                    id, body.Title, body.Icon, body.DisplayOrder, body.RequiredRole), ct);
                return Results.NoContent();
            }))
            .RequireAuthorization("SuperAdmin");

        group.MapPost("/modules/{id:guid}/active", (Guid id, SetActiveRequest body, ISender sender, CancellationToken ct) =>
            Exec(async () =>
            {
                await sender.Send(new SetModuleActiveCommand(id, body.IsActive), ct);
                return Results.NoContent();
            }))
            .RequireAuthorization("SuperAdmin");

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
        catch (ForbiddenException ex)
        {
            return Results.Json(new { message = ex.Message }, statusCode: StatusCodes.Status403Forbidden);
        }
    }

    // ---------- request bodies ----------
    public record CreateUserRequest(string UserName, string Email, string FullName, List<string>? Roles, string? InitialPassword);
    public record UpdateUserRequest(string UserName, string Email, string FullName, List<string>? Roles);

    public record CreateRoleRequest(string Name, string? Description);
    public record UpdateRoleRequest(string Name, string? Description);

    public record SaveMenuPermissionsRequest(Guid RoleId, List<Guid>? PermittedMenuIds);
    public record SaveModulePermissionsRequest(Guid RoleId, List<Guid>? PermittedModuleIds);

    public record CreateNumberingScopeRequest(string Code, string Name, string Prefix, NumberingCadence Cadence, byte Digits, bool ResetByOutlet);
    public record UpdateNumberingScopeRequest(string Name, string Prefix, NumberingCadence Cadence, byte Digits, bool ResetByOutlet);

    public record CreateTenantRequest(string Name, string Subdomain, string ContactEmail, DateTime? LicenseExpiresOnUtc);
    public record UpdateTenantRequest(string Name, string ContactEmail, DateTime? LicenseExpiresOnUtc);

    public record CreateModuleRequest(string Title, string? Icon, int? DisplayOrder, string? RequiredRole);
    public record UpdateModuleRequest(string Title, string? Icon, int DisplayOrder, string? RequiredRole);

    public record SetActiveRequest(bool IsActive);
}
