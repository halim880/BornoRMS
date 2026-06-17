using System.Security.Claims;
using BornoBit.Restaurant.Application.Menus;
using BornoBit.Restaurant.Domain.Identity;
using BornoBit.Restaurant.Infrastructure.Identity;
using MediatR;
using Microsoft.AspNetCore.Identity;

namespace BornoBit.Restaurant.Api.Endpoints;

/// <summary>
/// DB-driven navigation menu for the staff client, role-filtered like the Blazor sidebar.
/// The JWT carries role *names*, but <see cref="GetMenuTreeQuery"/> filters by role *Ids*,
/// so we resolve names → Ids via RoleManager (mirrors NavMenu.razor).
/// Mounted under the versioned group → GET /api/v1/staff/menu.
/// </summary>
public static class StaffMenuEndpoints
{
    public static IEndpointRouteBuilder MapStaffMenuEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/staff")
            .RequireCors("Frontends")
            .RequireAuthorization("Staff")
            .WithTags("StaffMenu");

        group.MapGet("/menu", async (
            ClaimsPrincipal principal,
            RoleManager<ApplicationRole> roles,
            ISender sender,
            CancellationToken ct) =>
        {
            var isSuperAdmin = principal.IsInRole(Roles.SuperAdmin);

            var roleIds = new List<Guid>();
            foreach (var name in principal.FindAll(ClaimTypes.Role).Select(c => c.Value).Distinct())
            {
                var role = await roles.FindByNameAsync(name);
                if (role is not null) roleIds.Add(role.Id);
            }

            var tree = await sender.Send(new GetMenuTreeQuery(roleIds, isSuperAdmin), ct);
            return Results.Ok(tree);
        });

        return app;
    }
}
