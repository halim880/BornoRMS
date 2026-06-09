using BornoBit.Restaurant.Domain.Identity;
using BornoBit.Restaurant.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace BornoBit.Restaurant.Infrastructure.Persistence.Seeding;

public class SuperAdminSeeder
{
    private readonly UserManager<ApplicationUser> _users;
    private readonly IConfiguration _config;
    private readonly ILogger<SuperAdminSeeder> _logger;

    public SuperAdminSeeder(UserManager<ApplicationUser> users, IConfiguration config, ILogger<SuperAdminSeeder> logger)
    {
        _users = users;
        _config = config;
        _logger = logger;
    }

    public async Task SeedAsync()
    {
        var section = _config.GetSection("Identity:SuperAdmin");
        var email = section["Email"];
        var password = section["Password"];
        var fullName = section["FullName"] ?? "Super Admin";
        var userName = section["UserName"];
        if (string.IsNullOrWhiteSpace(userName)) userName = email;

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(userName))
        {
            _logger.LogWarning("SuperAdminSeeder: Identity:SuperAdmin Email/Password missing; skipping.");
            return;
        }

        var existing = await _users.FindByEmailAsync(email);
        if (existing is not null)
        {
            if (!await _users.IsInRoleAsync(existing, Roles.SuperAdmin))
                await _users.AddToRoleAsync(existing, Roles.SuperAdmin);
            return;
        }

        var user = new ApplicationUser
        {
            UserName = userName,
            Email = email,
            EmailConfirmed = true,
            FullName = fullName,
            IsSuperAdmin = true
        };

        var create = await _users.CreateAsync(user, password);
        if (!create.Succeeded)
        {
            _logger.LogError("SuperAdminSeeder: failed to create super-admin: {Errors}",
                string.Join("; ", create.Errors.Select(e => e.Description)));
            return;
        }

        var addRole = await _users.AddToRoleAsync(user, Roles.SuperAdmin);
        if (!addRole.Succeeded)
        {
            _logger.LogError("SuperAdminSeeder: failed to assign SuperAdmin role: {Errors}",
                string.Join("; ", addRole.Errors.Select(e => e.Description)));
            return;
        }

        _logger.LogInformation("SuperAdminSeeder: created super-admin {Email}.", email);
    }
}
