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
            IRefreshTokenService refresh,
            CancellationToken ct) =>
        {
            var login = body.EmailOrUsername.Trim();
            var user = await users.FindByNameAsync(login) ?? await users.FindByEmailAsync(login);
            if (user is null || user.IsDeleted)
                return Results.Json(new { message = "Invalid credentials." }, statusCode: StatusCodes.Status401Unauthorized);

            if (!await users.CheckPasswordAsync(user, body.Password))
                return Results.Json(new { message = "Invalid credentials." }, statusCode: StatusCodes.Status401Unauthorized);

            var roles = await users.GetRolesAsync(user);
            var token = tokens.IssueAccessToken(user.Id, user.UserName ?? user.Email!, user.Email, user.FullName, roles);
            var refreshToken = await refresh.IssueAsync(user.Id, ct);

            return Results.Ok(new
            {
                accessToken = token.AccessToken,
                expiresAtUtc = token.ExpiresAtUtc,
                refreshToken = refreshToken.Token,
                refreshExpiresAtUtc = refreshToken.ExpiresAtUtc,
                user = new { id = user.Id, email = user.Email, fullName = user.FullName, roles }
            });
        })
        .RequireRateLimiting("auth");

        // Exchange a valid refresh token for a fresh access token (and a rotated refresh token).
        // The old refresh token is single-use — rotation guards against replay.
        group.MapPost("/refresh", async (
            RefreshRequest body,
            UserManager<ApplicationUser> users,
            IStaffTokenService tokens,
            IRefreshTokenService refresh,
            CancellationToken ct) =>
        {
            var rotation = await refresh.RotateAsync(body.RefreshToken ?? string.Empty, ct);
            if (rotation is null)
                return Results.Json(new { message = "Invalid or expired refresh token." }, statusCode: StatusCodes.Status401Unauthorized);

            var user = await users.FindByIdAsync(rotation.UserId.ToString());
            if (user is null || user.IsDeleted)
                return Results.Json(new { message = "Account is no longer active." }, statusCode: StatusCodes.Status401Unauthorized);

            var roles = await users.GetRolesAsync(user);
            var token = tokens.IssueAccessToken(user.Id, user.UserName ?? user.Email!, user.Email, user.FullName, roles);

            return Results.Ok(new
            {
                accessToken = token.AccessToken,
                expiresAtUtc = token.ExpiresAtUtc,
                refreshToken = rotation.Token,
                refreshExpiresAtUtc = rotation.ExpiresAtUtc,
                user = new { id = user.Id, email = user.Email, fullName = user.FullName, roles }
            });
        })
        .RequireRateLimiting("auth");

        // Best-effort revoke on sign-out so a stolen refresh token can't outlive the session.
        group.MapPost("/logout", async (RefreshRequest body, IRefreshTokenService refresh, CancellationToken ct) =>
        {
            await refresh.RevokeAsync(body.RefreshToken ?? string.Empty, ct);
            return Results.Ok(new { message = "Signed out." });
        });

        return app;
    }

    public record StaffLoginRequest(string EmailOrUsername, string Password);
    public record RefreshRequest(string? RefreshToken);
}
