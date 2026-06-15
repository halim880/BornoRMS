using BornoBit.Restaurant.Domain.Identity;
using BornoBit.Restaurant.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace BornoBit.Restaurant.Infrastructure.Persistence.Seeding;

public class StaffUserSeeder
{
    private readonly UserManager<ApplicationUser> _users;
    private readonly ILogger<StaffUserSeeder> _logger;

    public StaffUserSeeder(UserManager<ApplicationUser> users, ILogger<StaffUserSeeder> logger)
    {
        _users = users;
        _logger = logger;
    }

    private static readonly (string Email, string FullName, string Role, string Password)[] Defaults =
    {
        ("admin.user@bornobit.local", "Admin User",   Roles.Admin,   "ChangeMe!2026"),
        ("waiter@bornobit.local",     "Waiter User",  Roles.Waiter,  "ChangeMe!2026"),
        ("cashier@bornobit.local",    "Cashier User", Roles.Cashier, "ChangeMe!2026"),
        ("chef@bornobit.local",       "Chef User",    Roles.Chef,    "ChangeMe!2026"),
    };

    public async Task SeedAsync()
    {
        foreach (var (email, fullName, role, password) in Defaults)
        {
            var existing = await _users.FindByEmailAsync(email);
            if (existing is not null)
            {
                if (!await _users.IsInRoleAsync(existing, role))
                    await _users.AddToRoleAsync(existing, role);
                continue;
            }

            var user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true,
                FullName = fullName,
                IsSuperAdmin = false
            };

            var create = await _users.CreateAsync(user, password);
            if (!create.Succeeded)
            {
                _logger.LogError("StaffUserSeeder: failed to create {Email}: {Errors}",
                    email, string.Join("; ", create.Errors.Select(e => e.Description)));
                continue;
            }

            var addRole = await _users.AddToRoleAsync(user, role);
            if (!addRole.Succeeded)
            {
                _logger.LogError("StaffUserSeeder: failed to assign {Role} role to {Email}: {Errors}",
                    role, email, string.Join("; ", addRole.Errors.Select(e => e.Description)));
                continue;
            }

            _logger.LogInformation("StaffUserSeeder: created {Role} user {Email}.", role, email);
        }
    }
}
