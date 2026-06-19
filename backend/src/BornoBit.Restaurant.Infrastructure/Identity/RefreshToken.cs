namespace BornoBit.Restaurant.Infrastructure.Identity;

/// <summary>
/// A persisted, rotating staff refresh token. The raw token value is never stored — only its
/// SHA-256 hash (<see cref="TokenHash"/>) — so a database read can't be replayed as a credential.
/// Each successful refresh rotates the token: the presented one is revoked and a new one issued,
/// with <see cref="ReplacedByHash"/> linking the chain (used for reuse/theft detection).
/// </summary>
public class RefreshToken
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }

    /// <summary>SHA-256 (hex) of the raw token handed to the client.</summary>
    public string TokenHash { get; set; } = default!;

    public DateTime CreatedAtUtc { get; set; }
    public DateTime ExpiresAtUtc { get; set; }
    public DateTime? RevokedAtUtc { get; set; }

    /// <summary>Hash of the token that superseded this one on rotation (null until rotated).</summary>
    public string? ReplacedByHash { get; set; }

    public bool IsRevoked => RevokedAtUtc != null;
    public bool IsExpired(DateTime nowUtc) => nowUtc >= ExpiresAtUtc;
    public bool IsActive(DateTime nowUtc) => !IsRevoked && !IsExpired(nowUtc);
}
