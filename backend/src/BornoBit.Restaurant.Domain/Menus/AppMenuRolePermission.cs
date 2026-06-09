using BornoBit.Restaurant.Domain.Common;

namespace BornoBit.Restaurant.Domain.Menus;

public class AppMenuRolePermission : BaseEntity, IAuditableEntity
{
    public Guid MenuId { get; set; }
    public AppMenu? Menu { get; set; }
    public Guid RoleId { get; set; }

    public DateTime CreatedAtUtc { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
    public string? UpdatedBy { get; set; }
}
