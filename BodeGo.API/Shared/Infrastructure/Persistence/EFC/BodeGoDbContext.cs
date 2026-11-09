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
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
namespace BodeGo.API.Shared.Infrastructure.Persistence.EFC;

public class BodeGoDbContext(DbContextOptions<BodeGoDbContext> options) : DbContext(options)
{
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<ProductLot> ProductLots => Set<ProductLot>();
    public DbSet<Purchase> Purchases => Set<Purchase>();
    public DbSet<PurchaseItem> PurchaseItems => Set<PurchaseItem>();
    public DbSet<Sale> Sales => Set<Sale>();
    public DbSet<SaleItem> SaleItems => Set<SaleItem>();
    public DbSet<InventoryMovement> InventoryMovements => Set<InventoryMovement>();
    public DbSet<Waste> Wastes => Set<Waste>();
    public DbSet<InventoryCount> InventoryCounts => Set<InventoryCount>();
    public DbSet<InventoryCountItem> InventoryCountItems => Set<InventoryCountItem>();
    public DbSet<Alert> Alerts => Set<Alert>();

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder.Properties<Guid>().HaveConversion<GuidToStringConverter>().HaveColumnType("varchar(36)").HaveMaxLength(36);
        configurationBuilder.Properties<DateTime>().HaveColumnType("datetime(6)");
        configurationBuilder.Properties<DateOnly>().HaveColumnType("date");
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(BodeGoDbContext).Assembly);
        foreach (var entity in modelBuilder.Model.GetEntityTypes())
        foreach (var property in entity.GetProperties())
        {
            property.SetColumnName(Regex.Replace(property.Name, "([a-z0-9])([A-Z])", "$1_$2").ToLowerInvariant());
        }
    }

    private void ProtectHistory()
    {
        ChangeTracker.DetectChanges();
        if (ChangeTracker.Entries<InventoryMovement>().Any(e =>
                e.State is EntityState.Modified or EntityState.Deleted))
            throw new InvalidOperationException("Historical inventory movements cannot be changed or deleted.");
        if (ChangeTracker.Entries().Any(e => e.State == EntityState.Deleted &&
            e.Entity is Purchase or PurchaseItem or Sale or SaleItem or Waste or InventoryCount or InventoryCountItem))
            throw new InvalidOperationException("Business history must be preserved.");
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        ProtectHistory();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess,
        CancellationToken cancellationToken = default)
    {
        ProtectHistory();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }
}
