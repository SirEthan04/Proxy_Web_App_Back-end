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
namespace BodeGo.API.Monitoring.Infrastructure.Persistence.EFC.Configurations;

public class AlertConfiguration : IEntityTypeConfiguration<Alert>
{
    public void Configure(EntityTypeBuilder<Alert> builder)
    {
        builder.ToTable("alerts", table =>
        {
            table.HasCheckConstraint("ck_alerts_type", "alert_type IN ('LowStock', 'OutOfStock', 'Expiration')");
            table.HasCheckConstraint("ck_alerts_status", "status IN ('Pending', 'Attended')");
            table.HasCheckConstraint("ck_alerts_attendance", "(status = 'Pending' AND attended_at IS NULL AND attended_by_user_id IS NULL) OR (status = 'Attended' AND attended_at IS NOT NULL AND attended_by_user_id IS NOT NULL AND attended_at >= created_at)");
            table.HasCheckConstraint("ck_alerts_expiration", "alert_type <> 'Expiration' OR lot_id IS NOT NULL");
            table.HasCheckConstraint("ck_alerts_message", "CHAR_LENGTH(TRIM(message)) > 0");
        });
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.AlertType).HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(25).IsRequired();
        builder.Property(x => x.Message).HasMaxLength(255).IsRequired();
        builder.HasOne<Product>().WithMany().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ProductLot>().WithMany()
            .HasForeignKey(x => new { x.LotId, x.ProductId })
            .HasPrincipalKey(x => new { x.Id, x.ProductId })
            .IsRequired(false).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<User>().WithMany().HasForeignKey(x => x.AttendedByUserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => new { x.Status, x.AlertType, x.CreatedAt });
    }
}
