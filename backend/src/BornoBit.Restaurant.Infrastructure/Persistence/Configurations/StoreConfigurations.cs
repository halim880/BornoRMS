using BornoBit.Restaurant.Domain.Store;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BornoBit.Restaurant.Infrastructure.Persistence.Configurations;

public class StoreUnitConfiguration : IEntityTypeConfiguration<StoreUnit>
{
    public void Configure(EntityTypeBuilder<StoreUnit> builder)
    {
        builder.ToTable("StoreUnits");
        builder.HasKey(u => u.Id);

        builder.Property(u => u.Code).IsRequired().HasMaxLength(16);
        builder.Property(u => u.Name).IsRequired().HasMaxLength(80);
        builder.Property(u => u.BanglaName).HasMaxLength(80);
        builder.Property(u => u.Dimension).HasConversion<int>();
        builder.Property(u => u.ToBaseFactor).HasPrecision(18, 6);

        builder.HasIndex(u => u.Code).IsUnique();
    }
}

public class StoreCategoryConfiguration : IEntityTypeConfiguration<StoreCategory>
{
    public void Configure(EntityTypeBuilder<StoreCategory> builder)
    {
        builder.ToTable("StoreCategories");
        builder.HasKey(c => c.Id);

        builder.Property(c => c.Name).IsRequired().HasMaxLength(200);
        builder.Property(c => c.BanglaName).HasMaxLength(200);
        builder.Property(c => c.Description).HasMaxLength(1000);

        builder.HasIndex(c => c.DisplayOrder);
    }
}

public class StoreDepartmentConfiguration : IEntityTypeConfiguration<StoreDepartment>
{
    public void Configure(EntityTypeBuilder<StoreDepartment> builder)
    {
        builder.ToTable("StoreDepartments");
        builder.HasKey(d => d.Id);

        builder.Property(d => d.Code).IsRequired().HasMaxLength(40);
        builder.Property(d => d.Name).IsRequired().HasMaxLength(200);
        builder.Property(d => d.BanglaName).HasMaxLength(200);
        builder.Property(d => d.Description).HasMaxLength(1000);

        builder.HasIndex(d => d.Code).IsUnique();
        builder.HasIndex(d => d.DisplayOrder);
    }
}

public class StoreItemConfiguration : IEntityTypeConfiguration<StoreItem>
{
    public void Configure(EntityTypeBuilder<StoreItem> builder)
    {
        builder.ToTable("StoreItems");
        builder.HasKey(i => i.Id);

        builder.Property(i => i.Code).IsRequired().HasMaxLength(40);
        builder.Property(i => i.Name).IsRequired().HasMaxLength(200);
        builder.Property(i => i.BanglaName).HasMaxLength(200);
        builder.Property(i => i.Currency).IsRequired().HasMaxLength(8);
        builder.Property(i => i.PackNote).HasMaxLength(200);

        builder.Property(i => i.QtyOnHand).HasPrecision(18, 3);
        builder.Property(i => i.ReorderLevel).HasPrecision(18, 3);
        builder.Property(i => i.ReorderQty).HasPrecision(18, 3);
        builder.Property(i => i.PackSize).HasPrecision(18, 3);
        builder.Property(i => i.AvgCost).HasPrecision(18, 2);

        builder.Ignore(i => i.IsLowStock);
        builder.Ignore(i => i.StockValue);

        builder.HasIndex(i => i.Code).IsUnique();
        builder.HasIndex(i => i.StoreCategoryId);

        builder.HasOne<StoreCategory>()
            .WithMany()
            .HasForeignKey(i => i.StoreCategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<StoreUnit>()
            .WithMany()
            .HasForeignKey(i => i.BaseUnitId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class StoreSupplierConfiguration : IEntityTypeConfiguration<StoreSupplier>
{
    public void Configure(EntityTypeBuilder<StoreSupplier> builder)
    {
        builder.ToTable("StoreSuppliers");
        builder.HasKey(s => s.Id);

        builder.Property(s => s.Code).IsRequired().HasMaxLength(40);
        builder.Property(s => s.Name).IsRequired().HasMaxLength(200);
        builder.Property(s => s.Phone).HasMaxLength(40);
        builder.Property(s => s.Address).HasMaxLength(500);
        builder.Property(s => s.Notes).HasMaxLength(1000);

        builder.HasIndex(s => s.Code).IsUnique();
    }
}

public class StoreGoodsReceiptConfiguration : IEntityTypeConfiguration<StoreGoodsReceipt>
{
    public void Configure(EntityTypeBuilder<StoreGoodsReceipt> builder)
    {
        builder.ToTable("StoreGoodsReceipts");
        builder.HasKey(g => g.Id);

        builder.Property(g => g.GrnNumber).IsRequired().HasMaxLength(40);
        builder.Property(g => g.InvoiceNo).HasMaxLength(80);
        builder.Property(g => g.Currency).IsRequired().HasMaxLength(8);
        builder.Property(g => g.Notes).HasMaxLength(1000);
        builder.Property(g => g.Status).HasConversion<int>();

        builder.Ignore(g => g.Subtotal);

        builder.HasIndex(g => g.GrnNumber).IsUnique();
        builder.HasIndex(g => g.StoreSupplierId);
        builder.HasIndex(g => g.Status);

        builder.HasOne<StoreSupplier>()
            .WithMany()
            .HasForeignKey(g => g.StoreSupplierId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(g => g.Lines)
            .WithOne()
            .HasForeignKey(l => l.StoreGoodsReceiptId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Metadata.FindNavigation(nameof(StoreGoodsReceipt.Lines))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);
    }
}

public class StoreGoodsReceiptLineConfiguration : IEntityTypeConfiguration<StoreGoodsReceiptLine>
{
    public void Configure(EntityTypeBuilder<StoreGoodsReceiptLine> builder)
    {
        builder.ToTable("StoreGoodsReceiptLines");
        builder.HasKey(l => l.Id);

        builder.Property(l => l.ItemName).IsRequired().HasMaxLength(200);
        builder.Property(l => l.Qty).HasPrecision(18, 3);
        builder.Property(l => l.QtyBase).HasPrecision(18, 3);
        builder.Property(l => l.UnitCost).HasPrecision(18, 2);

        builder.Ignore(l => l.LineTotal);

        builder.HasIndex(l => l.StoreGoodsReceiptId);

        builder.HasOne<StoreItem>()
            .WithMany()
            .HasForeignKey(l => l.StoreItemId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<StoreUnit>()
            .WithMany()
            .HasForeignKey(l => l.UnitId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class StoreIssueConfiguration : IEntityTypeConfiguration<StoreIssue>
{
    public void Configure(EntityTypeBuilder<StoreIssue> builder)
    {
        builder.ToTable("StoreIssues");
        builder.HasKey(g => g.Id);

        builder.Property(g => g.IssueNumber).IsRequired().HasMaxLength(40);
        builder.Property(g => g.Destination).IsRequired().HasMaxLength(200);
        builder.Property(g => g.Notes).HasMaxLength(1000);
        builder.Property(g => g.Status).HasConversion<int>();

        builder.HasIndex(g => g.IssueNumber).IsUnique();
        builder.HasIndex(g => g.Status);
        builder.HasIndex(g => g.StoreDepartmentId);
        builder.HasIndex(g => g.StoreRequisitionId);

        builder.HasOne<StoreDepartment>()
            .WithMany()
            .HasForeignKey(g => g.StoreDepartmentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<StoreRequisition>()
            .WithMany()
            .HasForeignKey(g => g.StoreRequisitionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(g => g.Lines)
            .WithOne()
            .HasForeignKey(l => l.StoreIssueId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Metadata.FindNavigation(nameof(StoreIssue.Lines))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);
    }
}

public class StoreRequisitionConfiguration : IEntityTypeConfiguration<StoreRequisition>
{
    public void Configure(EntityTypeBuilder<StoreRequisition> builder)
    {
        builder.ToTable("StoreRequisitions");
        builder.HasKey(r => r.Id);

        builder.Property(r => r.RequisitionNumber).IsRequired().HasMaxLength(40);
        builder.Property(r => r.Notes).HasMaxLength(1000);
        builder.Property(r => r.RejectedReason).HasMaxLength(500);
        builder.Property(r => r.Status).HasConversion<int>();

        builder.HasIndex(r => r.RequisitionNumber).IsUnique();
        builder.HasIndex(r => r.Status);
        builder.HasIndex(r => r.StoreDepartmentId);

        builder.HasOne<StoreDepartment>()
            .WithMany()
            .HasForeignKey(r => r.StoreDepartmentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(r => r.Lines)
            .WithOne()
            .HasForeignKey(l => l.StoreRequisitionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Metadata.FindNavigation(nameof(StoreRequisition.Lines))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);
    }
}

public class StoreRequisitionLineConfiguration : IEntityTypeConfiguration<StoreRequisitionLine>
{
    public void Configure(EntityTypeBuilder<StoreRequisitionLine> builder)
    {
        builder.ToTable("StoreRequisitionLines");
        builder.HasKey(l => l.Id);

        builder.Property(l => l.ItemName).IsRequired().HasMaxLength(200);
        builder.Property(l => l.RequestedQty).HasPrecision(18, 3);
        builder.Property(l => l.RequestedQtyBase).HasPrecision(18, 3);
        builder.Property(l => l.ApprovedQtyBase).HasPrecision(18, 3);
        builder.Property(l => l.IssuedQtyBase).HasPrecision(18, 3);

        builder.Ignore(l => l.OutstandingQtyBase);

        builder.HasIndex(l => l.StoreRequisitionId);

        builder.HasOne<StoreItem>()
            .WithMany()
            .HasForeignKey(l => l.StoreItemId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<StoreUnit>()
            .WithMany()
            .HasForeignKey(l => l.UnitId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class StoreIssueLineConfiguration : IEntityTypeConfiguration<StoreIssueLine>
{
    public void Configure(EntityTypeBuilder<StoreIssueLine> builder)
    {
        builder.ToTable("StoreIssueLines");
        builder.HasKey(l => l.Id);

        builder.Property(l => l.ItemName).IsRequired().HasMaxLength(200);
        builder.Property(l => l.Qty).HasPrecision(18, 3);
        builder.Property(l => l.QtyBase).HasPrecision(18, 3);

        builder.HasIndex(l => l.StoreIssueId);

        builder.HasOne<StoreItem>()
            .WithMany()
            .HasForeignKey(l => l.StoreItemId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<StoreUnit>()
            .WithMany()
            .HasForeignKey(l => l.UnitId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class StorePaymentConfiguration : IEntityTypeConfiguration<StorePayment>
{
    public void Configure(EntityTypeBuilder<StorePayment> builder)
    {
        builder.ToTable("StorePayments");
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Amount).HasPrecision(18, 2);
        builder.Property(p => p.Method).HasConversion<int>();
        builder.Property(p => p.Reference).HasMaxLength(120);
        builder.Property(p => p.Notes).HasMaxLength(1000);

        builder.HasIndex(p => p.StoreSupplierId);
        builder.HasIndex(p => p.PaidAtUtc);

        builder.HasOne<StoreSupplier>()
            .WithMany()
            .HasForeignKey(p => p.StoreSupplierId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class StoreStockMovementConfiguration : IEntityTypeConfiguration<StoreStockMovement>
{
    public void Configure(EntityTypeBuilder<StoreStockMovement> builder)
    {
        builder.ToTable("StoreStockMovements");
        builder.HasKey(m => m.Id);

        builder.Property(m => m.MovementType).HasConversion<int>();
        builder.Property(m => m.QtyBase).HasPrecision(18, 3);
        builder.Property(m => m.UnitCost).HasPrecision(18, 2);
        builder.Property(m => m.Reason).HasMaxLength(500);
        builder.Property(m => m.ReferenceType).HasMaxLength(80);

        builder.HasIndex(m => m.StoreItemId);
        builder.HasIndex(m => m.OccurredAtUtc);
        builder.HasIndex(m => m.MovementType);

        builder.HasOne<StoreItem>()
            .WithMany()
            .HasForeignKey(m => m.StoreItemId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
