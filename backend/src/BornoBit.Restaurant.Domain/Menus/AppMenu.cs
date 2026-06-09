using BornoBit.Restaurant.Domain.Common;

namespace BornoBit.Restaurant.Domain.Menus;

public class AppMenu : BaseEntity, IAuditableEntity, ISoftDelete
{
    public string Title { get; set; } = string.Empty;
    public string? Url { get; set; }
    public string? Icon { get; set; }
    public Guid? ParentId { get; set; }
    public AppMenu? Parent { get; set; }
    public ICollection<AppMenu> Children { get; set; } = new List<AppMenu>();
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; } = true;
    public string? RequiredRole { get; set; }

    public DateTime CreatedAtUtc { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
    public string? UpdatedBy { get; set; }

    public bool IsDeleted { get; set; }
    public DateTime? DeletedAtUtc { get; set; }
    public string? DeletedBy { get; set; }
}
