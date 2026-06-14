using BornoBit.Restaurant.Application.Common.Security;
using BornoBit.Restaurant.Domain.Identity;
using BornoBit.Restaurant.Shared.Common;
using Microsoft.AspNetCore.Identity;

namespace BornoBit.Restaurant.Infrastructure.Identity;

/// <summary>
/// Validates a manager credential at the point of a sensitive POS action. Confirms the password and that
/// the account carries a manager-grade role (SuperAdmin / Admin / Manager). Never signs the manager in —
/// it only authorizes the one action the cashier is performing.
/// </summary>
public sealed class ManagerApprovalService : IManagerApprovalService
{
    private readonly UserManager<ApplicationUser> _users;

    public ManagerApprovalService(UserManager<ApplicationUser> users) => _users = users;

    public async Task<ManagerApprover> AuthorizeAsync(string userName, string password, CancellationToken cancellationToken = default)
    {
        var user = await _users.FindByNameAsync(userName) ?? await _users.FindByEmailAsync(userName);
        if (user is null || user.IsDeleted || !await _users.CheckPasswordAsync(user, password))
            throw new ForbiddenException("Manager credentials were not recognized.");

        var roles = await _users.GetRolesAsync(user);
        var isManager = user.IsSuperAdmin
            || roles.Contains(Roles.SuperAdmin) || roles.Contains(Roles.Admin) || roles.Contains(Roles.Manager);

        if (!isManager)
            throw new ForbiddenException($"{user.FullName ?? user.UserName} is not authorized to approve this action.");

        return new ManagerApprover(user.Id, string.IsNullOrWhiteSpace(user.FullName) ? user.UserName! : user.FullName);
    }
}
