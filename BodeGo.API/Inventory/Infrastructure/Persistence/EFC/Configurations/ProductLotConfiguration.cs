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
namespace BodeGo.API.Inventory.Infrastructure.Persistence.EFC.Configurations;

public class ProductLotConfiguration : IEntityTypeConfiguration<ProductLot>
{
    public void Configure(EntityTypeBuilder<ProductLot> builder)
    {
        builder.ToTable("product_lots", table =>
        {
            table.HasCheckConstraint("ck_product_lots_quantity", "quantity_available >= 0");
            table.HasCheckConstraint("ck_product_lots_code", "CHAR_LENGTH(TRIM(lot_code)) > 0");
        });
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.LotCode).HasMaxLength(80).IsRequired();
        builder.HasAlternateKey(x => new { x.Id, x.ProductId });
        builder.HasIndex(x => new { x.ProductId, x.LotCode }).IsUnique();
        builder.HasIndex(x => x.ExpirationDate);
    }
}
