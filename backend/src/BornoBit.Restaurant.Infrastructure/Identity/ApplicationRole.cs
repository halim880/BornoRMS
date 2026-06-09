using Microsoft.AspNetCore.Identity;

namespace BornoBit.Restaurant.Infrastructure.Identity;

public class ApplicationRole : IdentityRole<Guid>
{
    public string? Description { get; set; }

    public ApplicationRole() { }
    public ApplicationRole(string roleName) : base(roleName) { }
}
