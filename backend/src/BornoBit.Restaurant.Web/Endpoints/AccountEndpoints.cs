using BornoBit.Restaurant.Application.Modules;
using BornoBit.Restaurant.Infrastructure.Identity;
using MediatR;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace BornoBit.Restaurant.Web.Endpoints;

public static class AccountEndpoints
{
    public static IEndpointRouteBuilder MapAccountEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/account/login", async (
            HttpContext ctx,
            UserManager<ApplicationUser> users,
            RoleManager<ApplicationRole> rolesMgr,
            ISender mediator,
            [FromForm] string? userName,
            [FromForm] string? password,
            [FromForm] string? returnUrl) =>
        {
            string Fail(string code, string? rt) =>
                $"/login?error={code}" + (string.IsNullOrWhiteSpace(rt) ? "" : $"&returnUrl={Uri.EscapeDataString(rt)}");

            if (string.IsNullOrWhiteSpace(userName) || string.IsNullOrWhiteSpace(password))
                return Results.Redirect(Fail("missing", returnUrl));

            var name = userName.Trim();
            var user = await users.FindByNameAsync(name) ?? await users.FindByEmailAsync(name);
            if (user is null || user.IsDeleted || !await users.CheckPasswordAsync(user, password))
                return Results.Redirect(Fail("invalid", returnUrl));

            var roles = await users.GetRolesAsync(user);
            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new(ClaimTypes.Name, user.UserName ?? user.Email!),
                new(ClaimTypes.Email, user.Email ?? string.Empty)
            };
            foreach (var r in roles) claims.Add(new Claim(ClaimTypes.Role, r));
            if (user.IsSuperAdmin) claims.Add(new Claim("super_admin", "true"));

            var identity = new ClaimsIdentity(claims, "Cookies");
            await ctx.SignInAsync("Cookies", new ClaimsPrincipal(identity));

            // Honor an explicit deep-link returnUrl first; otherwise land on the first menu URL of the
            // user's lowest-display-order project module (same source the sidebar/ModuleSwitcher uses).
            // Treat "/" as no return — it only round-trips back through Home → default landing.
            if (!string.IsNullOrWhiteSpace(returnUrl) && returnUrl != "/"
                && Uri.IsWellFormedUriString(returnUrl, UriKind.Relative))
                return Results.Redirect(returnUrl!);

            var roleIds = new List<Guid>();
            if (!user.IsSuperAdmin)
            {
                foreach (var r in roles)
                {
                    var role = await rolesMgr.FindByNameAsync(r);
                    if (role is not null) roleIds.Add(role.Id);
                }
            }

            var modules = await mediator.Send(new GetUserModulesQuery(roleIds, user.IsSuperAdmin));
            var landing = modules
                .OrderBy(m => m.DisplayOrder)
                .Select(m => m.FirstAccessibleUrl)
                .FirstOrDefault(u => !string.IsNullOrWhiteSpace(u));

            return Results.Redirect(landing ?? "/orders");
        }).DisableAntiforgery();

        app.MapGet("/logout", async (HttpContext ctx) =>
        {
            await ctx.SignOutAsync("Cookies");
            return Results.Redirect("/login");
        }).WithName("Logout");

        return app;
    }
}
