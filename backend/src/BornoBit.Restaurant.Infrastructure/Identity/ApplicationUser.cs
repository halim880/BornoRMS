using BornoBit.Restaurant.Domain.Common;
using Microsoft.AspNetCore.Identity;

namespace BornoBit.Restaurant.Infrastructure.Identity;

public class ApplicationUser : IdentityUser<Guid>, IAuditableEntity, ISoftDelete
{
    public string FullName { get; set; } = string.Empty;
    public bool IsSuperAdmin { get; set; }

    public DateTime CreatedAtUtc { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
    public string? UpdatedBy { get; set; }

    public bool IsDeleted { get; set; }
    public DateTime? DeletedAtUtc { get; set; }
    public string? DeletedBy { get; set; }
}
