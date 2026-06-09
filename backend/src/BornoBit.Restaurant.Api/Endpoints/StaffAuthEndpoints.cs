using BornoBit.Restaurant.Application.Common.Identity;
using BornoBit.Restaurant.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;

namespace BornoBit.Restaurant.Api.Endpoints;

public static class StaffAuthEndpoints
{
    public static IEndpointRouteBuilder MapStaffAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/staff/auth")
            .RequireCors("Frontends")
            .WithTags("StaffAuth");

        group.MapPost("/login", async (
            StaffLoginRequest body,
            UserManager<ApplicationUser> users,
            IStaffTokenService tokens,
            CancellationToken ct) =>
        {
            var user = await users.FindByEmailAsync(body.Email);
            if (user is null || user.IsDeleted)
                return Results.Json(new { message = "Invalid credentials." }, statusCode: StatusCodes.Status401Unauthorized);

            if (!await users.CheckPasswordAsync(user, body.Password))
                return Results.Json(new { message = "Invalid credentials." }, statusCode: StatusCodes.Status401Unauthorized);

            var roles = await users.GetRolesAsync(user);
            var token = tokens.IssueAccessToken(user.Id, user.UserName ?? user.Email!, user.Email, user.FullName, roles);

            return Results.Ok(new
            {
                accessToken = token.AccessToken,
                expiresAtUtc = token.ExpiresAtUtc,
                user = new { id = user.Id, email = user.Email, fullName = user.FullName, roles }
            });
        });

        return app;
    }

    public record StaffLoginRequest(string Email, string Password);
}
