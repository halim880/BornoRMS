using System.Security.Cryptography;
using System.Text;
using BornoBit.Restaurant.Application.Common.Identity;
using BornoBit.Restaurant.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace BornoBit.Restaurant.Infrastructure.Identity;

/// <summary>
/// DB-backed rotating refresh tokens. Raw tokens are 256 bits of CSPRNG randomness handed to the
/// client; only their SHA-256 hash is persisted. Lifetime is <c>Jwt:RefreshTokenDays</c> (default 14).
/// </summary>
public class RefreshTokenService : IRefreshTokenService
{
    private readonly ApplicationDbContext _db;
    private readonly TimeProvider _timeProvider;
    private readonly int _lifetimeDays;

    public RefreshTokenService(ApplicationDbContext db, TimeProvider timeProvider, IConfiguration configuration)
    {
        _db = db;
        _timeProvider = timeProvider;
        _lifetimeDays = int.TryParse(configuration.GetSection("Jwt")["RefreshTokenDays"], out var d) && d > 0 ? d : 14;
    }

    public async Task<RefreshTokenResult> IssueAsync(Guid userId, CancellationToken ct)
    {
        var (raw, hash) = NewToken();
        var now = _timeProvider.GetUtcNow().UtcDateTime;
        var expires = now.AddDays(_lifetimeDays);

        _db.RefreshTokens.Add(new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TokenHash = hash,
            CreatedAtUtc = now,
            ExpiresAtUtc = expires
        });
        await _db.SaveChangesAsync(ct);

        return new RefreshTokenResult(raw, expires);
    }

    public async Task<RefreshRotation?> RotateAsync(string rawToken, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(rawToken)) return null;
        var hash = Hash(rawToken);
        var now = _timeProvider.GetUtcNow().UtcDateTime;

        var existing = await _db.RefreshTokens.FirstOrDefaultAsync(t => t.TokenHash == hash, ct);
        if (existing is null) return null;

        // Replay of an already-revoked token => likely theft of the chain. Burn every active token
        // the user has, forcing a fresh login everywhere.
        if (existing.IsRevoked)
        {
            await RevokeAllActiveAsync(existing.UserId, now, ct);
            return null;
        }

        if (existing.IsExpired(now)) return null;

        var (raw, newHash) = NewToken();
        var expires = now.AddDays(_lifetimeDays);

        existing.RevokedAtUtc = now;
        existing.ReplacedByHash = newHash;

        _db.RefreshTokens.Add(new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = existing.UserId,
            TokenHash = newHash,
            CreatedAtUtc = now,
            ExpiresAtUtc = expires
        });
        await _db.SaveChangesAsync(ct);

        return new RefreshRotation(existing.UserId, raw, expires);
    }

    public async Task RevokeAsync(string rawToken, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(rawToken)) return;
        var hash = Hash(rawToken);
        var now = _timeProvider.GetUtcNow().UtcDateTime;

        var existing = await _db.RefreshTokens.FirstOrDefaultAsync(t => t.TokenHash == hash, ct);
        if (existing is null || existing.IsRevoked) return;

        existing.RevokedAtUtc = now;
        await _db.SaveChangesAsync(ct);
    }

    private async Task RevokeAllActiveAsync(Guid userId, DateTime now, CancellationToken ct)
    {
        var active = await _db.RefreshTokens
            .Where(t => t.UserId == userId && t.RevokedAtUtc == null && t.ExpiresAtUtc > now)
            .ToListAsync(ct);
        foreach (var t in active) t.RevokedAtUtc = now;
        if (active.Count > 0) await _db.SaveChangesAsync(ct);
    }

    private static (string Raw, string Hash) NewToken()
    {
        var raw = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        return (raw, Hash(raw));
    }

    private static string Hash(string raw) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(raw)));
}
