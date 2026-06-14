using BornoBit.Restaurant.Domain.Identity;
using BornoBit.Restaurant.Shared.Common;
using BornoBit.Restaurant.Shared.Identity;

namespace BornoBit.Restaurant.Application.Common.Security;

/// <summary>
/// Handler-level role guard for sensitive operations (discount, void, settlement, session close). The page
/// layer already hides the buttons via &lt;AuthorizeView&gt;; this is the server-side backstop so a crafted
/// request can't bypass it. SuperAdmin always passes. Throws <see cref="ForbiddenException"/> on denial.
/// </summary>
public static class PermissionGuard
{
    public static void Require(ICurrentUser user, params string[] allowedRoles)
    {
        if (user.IsInRole(Roles.SuperAdmin)) return;
        foreach (var role in allowedRoles)
            if (user.IsInRole(role)) return;

        throw new ForbiddenException("You are not authorized to perform this action.");
    }
}
