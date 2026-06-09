using BornoBit.Restaurant.Domain.Menus;
using BornoBit.Restaurant.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BornoBit.Restaurant.Infrastructure.Persistence.Configurations;

public class AppMenuRolePermissionConfiguration : IEntityTypeConfiguration<AppMenuRolePermission>
{
    public void Configure(EntityTypeBuilder<AppMenuRolePermission> builder)
    {
        builder.ToTable("AppMenuRolePermissions");
        builder.HasKey(p => p.Id);

        builder.HasOne(p => p.Menu)
            .WithMany()
            .HasForeignKey(p => p.MenuId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<ApplicationRole>()
            .WithMany()
            .HasForeignKey(p => p.RoleId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(p => new { p.MenuId, p.RoleId }).IsUnique();
        builder.HasIndex(p => p.RoleId);
    }
}
