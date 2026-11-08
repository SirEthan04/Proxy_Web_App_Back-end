using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using BodeGo.API.IAM.Domain.Model.Aggregates;
using BodeGo.API.IAM.Domain.Model.Entities;
using BodeGo.API.Inventory.Domain.Model.Aggregates;
using BodeGo.API.Inventory.Domain.Model.Entities;
using BodeGo.API.Commercial.Domain.Model.Aggregates;
using BodeGo.API.Commercial.Domain.Model.Entities;
using BodeGo.API.Operations.Domain.Model.Aggregates;
using BodeGo.API.Operations.Domain.Model.Entities;
using BodeGo.API.Monitoring.Domain.Model.Aggregates;
namespace BodeGo.API.Operations.Infrastructure.Persistence.EFC.Configurations;

public class InventoryMovementConfiguration : IEntityTypeConfiguration<InventoryMovement>
{
    public void Configure(EntityTypeBuilder<InventoryMovement> builder)
    {
        builder.ToTable("inventory_movements", table =>
        {
            table.HasCheckConstraint("ck_inventory_movements_quantity", "quantity_delta <> 0");
            table.HasCheckConstraint("ck_inventory_movements_type", "movement_type IN ('Purchase', 'Sale', 'Waste', 'CountAdjustment')");
            table.HasCheckConstraint("ck_inventory_movements_sign", "(movement_type = 'Purchase' AND quantity_delta > 0) OR (movement_type IN ('Sale', 'Waste') AND quantity_delta < 0) OR movement_type = 'CountAdjustment'");
            table.HasCheckConstraint("ck_inventory_movements_source", "CHAR_LENGTH(TRIM(source_type)) > 0 AND CHAR_LENGTH(source_id) = 36");
        });
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.MovementType).HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.Property(x => x.SourceType).HasMaxLength(30).IsRequired();
        builder.Property(x => x.SourceId).HasMaxLength(36).IsRequired();
        builder.HasOne<User>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Product>().WithMany().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ProductLot>().WithMany()
            .HasForeignKey(x => new { x.LotId, x.ProductId })
            .HasPrincipalKey(x => new { x.Id, x.ProductId })
            .IsRequired(false).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => new { x.ProductId, x.CreatedAt });
        builder.HasIndex(x => new { x.SourceType, x.SourceId });
    }
}
