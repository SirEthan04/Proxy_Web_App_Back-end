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

public class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("products", table =>
        {
            table.HasCheckConstraint("ck_products_name", "CHAR_LENGTH(TRIM(name)) > 0");
            table.HasCheckConstraint("ck_products_price", "sale_price >= 0");
            table.HasCheckConstraint("ck_products_stock", "stock >= 0 AND minimum_stock >= 0");
            table.HasCheckConstraint("ck_products_version", "version >= 0");
        });
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.Name).HasConversion(x => x.Value,
            x => new BodeGo.API.Inventory.Domain.Model.ValueObjects.ProductName(x)).HasMaxLength(150).IsRequired();
        builder.Property(x => x.SalePrice).HasConversion(x => x.Value,
            x => new BodeGo.API.Inventory.Domain.Model.ValueObjects.Money(x)).HasPrecision(12, 2).IsRequired();
        builder.Property(x => x.Stock).HasConversion(x => x.Value,
            x => new BodeGo.API.Inventory.Domain.Model.ValueObjects.StockLevel(x)).IsRequired();
        builder.Property(x => x.MinimumStock).HasConversion(x => x.Value,
            x => new BodeGo.API.Inventory.Domain.Model.ValueObjects.StockLevel(x)).IsRequired();
        builder.Property(x => x.Version).IsConcurrencyToken();
        builder.HasOne<Category>().WithMany().HasForeignKey(x => x.CategoryId).OnDelete(DeleteBehavior.Restrict);
        builder.HasMany(x => x.Lots).WithOne().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Restrict);
        builder.Navigation(x => x.Lots).HasField("_lots").UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
