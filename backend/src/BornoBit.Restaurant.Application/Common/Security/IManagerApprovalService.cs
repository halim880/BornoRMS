using BornoBit.Restaurant.Domain.Identity;
using BornoBit.Restaurant.Shared.Common;
using BornoBit.Restaurant.Shared.Identity;

namespace BornoBit.Restaurant.Application.Common.Security;

/// <summary>A manager who authorized a privileged action at the terminal (instant-override flow).</summary>
public record ManagerApprover(Guid UserId, string Name);

/// <summary>
/// Validates a manager's credentials at the point of a sensitive action (large discount, void, refund)
/// so a cashier can get an on-the-spot override without switching login. Implemented in Infrastructure
/// against ASP.NET Identity. Throws <see cref="ForbiddenException"/> on bad credentials or a non-manager.
/// </summary>
public interface IManagerApprovalService
{
    Task<ManagerApprover> AuthorizeAsync(string userName, string password, CancellationToken cancellationToken = default);
}

public static class ManagerApprovalResolver
{
    /// <summary>
    /// Resolves who authorizes a manager-gated action. If the current user already holds a manager role,
    /// they are the authorizer (returns null — no separate approver). Otherwise a manager credential must be
    /// supplied and is validated server-side; the approver's name is returned for the audit trail.
    /// </summary>
    public static async Task<string?> ResolveApproverAsync(
        ICurrentUser currentUser,
        IManagerApprovalService approvals,
        string? managerUserName,
        string? managerPassword,
        CancellationToken cancellationToken)
    {
        if (currentUser.IsInRole(Roles.SuperAdmin) || currentUser.IsInRole(Roles.Admin) || currentUser.IsInRole(Roles.Manager))
            return null;

        if (string.IsNullOrWhiteSpace(managerUserName) || string.IsNullOrWhiteSpace(managerPassword))
            throw new ForbiddenException("Manager authorization is required for this action.");

        var approver = await approvals.AuthorizeAsync(managerUserName, managerPassword, cancellationToken);
        return approver.Name;
    }

    /// <summary>Folds the approver into an audit reason, e.g. "wrong item (approved by Jane M.)".</summary>
    public static string WithApprover(string reason, string? approverName) =>
        string.IsNullOrWhiteSpace(approverName) ? reason : $"{reason} (approved by {approverName})";
}
