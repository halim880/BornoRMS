namespace BornoBit.Restaurant.Application.Common.Identity;

public record RefreshTokenResult(string Token, DateTime ExpiresAtUtc);

/// <summary>Outcome of a successful rotation: the user the token belongs to plus the freshly issued
/// replacement token. Null from <see cref="IRefreshTokenService.RotateAsync"/> means the presented
/// token was unknown, expired, or already used.</summary>
public record RefreshRotation(Guid UserId, string Token, DateTime ExpiresAtUtc);

public interface IRefreshTokenService
{
    /// <summary>Mint a new refresh token for a user (called at login).</summary>
    Task<RefreshTokenResult> IssueAsync(Guid userId, CancellationToken ct);

    /// <summary>Validate a raw refresh token and, if active, rotate it (revoke the old, issue a new).
    /// Returns null when the token is unknown/expired/revoked. A revoked-token replay is treated as
    /// theft and revokes the user's whole active token set.</summary>
    Task<RefreshRotation?> RotateAsync(string rawToken, CancellationToken ct);

    /// <summary>Revoke a raw refresh token (called at logout). No-op if unknown.</summary>
    Task RevokeAsync(string rawToken, CancellationToken ct);
}
