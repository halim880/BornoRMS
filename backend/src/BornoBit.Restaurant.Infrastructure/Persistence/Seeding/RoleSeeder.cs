using BornoBit.Restaurant.Domain.Identity;
using BornoBit.Restaurant.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace BornoBit.Restaurant.Infrastructure.Persistence.Seeding;

public class RoleSeeder
{
    private readonly RoleManager<ApplicationRole> _roles;
    private readonly ILogger<RoleSeeder> _logger;

    public RoleSeeder(RoleManager<ApplicationRole> roles, ILogger<RoleSeeder> logger)
    {
        _roles = roles;
        _logger = logger;
    }

    public async Task SeedAsync()
    {
        foreach (var roleName in Roles.All)
        {
            if (await _roles.RoleExistsAsync(roleName)) continue;
            var role = new ApplicationRole(roleName) { Description = $"Built-in {roleName} role." };
            var result = await _roles.CreateAsync(role);
            if (!result.Succeeded)
            {
                _logger.LogWarning("RoleSeeder: failed to create role {Role}: {Errors}",
                    roleName, string.Join("; ", result.Errors.Select(e => e.Description)));
            }
            else
            {
                _logger.LogInformation("RoleSeeder: created role {Role}.", roleName);
            }
        }
    }
}
