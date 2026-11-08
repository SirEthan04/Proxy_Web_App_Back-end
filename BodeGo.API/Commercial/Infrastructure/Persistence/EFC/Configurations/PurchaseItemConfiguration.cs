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
namespace BodeGo.API.Commercial.Infrastructure.Persistence.EFC.Configurations;

public class PurchaseItemConfiguration : IEntityTypeConfiguration<PurchaseItem>
{
    public void Configure(EntityTypeBuilder<PurchaseItem> builder)
    {
        builder.ToTable("purchase_items", table =>
        {
            table.HasCheckConstraint("ck_purchase_items_amounts", "quantity > 0 AND unit_cost >= 0 AND subtotal = quantity * unit_cost");
        });
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.Quantity).HasConversion(x => x.Value,
            x => new BodeGo.API.Commercial.Domain.Model.ValueObjects.Quantity(x)).IsRequired();
        builder.Property(x => x.UnitCost).HasConversion(x => x.Value,
            x => new BodeGo.API.Commercial.Domain.Model.ValueObjects.Money(x)).HasPrecision(12, 2).IsRequired();
        builder.Property(x => x.Subtotal).HasConversion(x => x.Value,
            x => new BodeGo.API.Commercial.Domain.Model.ValueObjects.Money(x)).HasPrecision(12, 2).IsRequired();
        builder.HasOne<Product>().WithMany().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ProductLot>().WithMany()
            .HasForeignKey(x => new { x.LotId, x.ProductId })
            .HasPrincipalKey(x => new { x.Id, x.ProductId })
            .IsRequired(false).OnDelete(DeleteBehavior.Restrict);
    }
}
