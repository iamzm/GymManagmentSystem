using Domin.GymEntities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Presistence.Data.Configurations {
    public class InventoryItemConfiguration : IEntityTypeConfiguration<InventoryItem> {
        public void Configure(EntityTypeBuilder<InventoryItem> builder) {

            builder.Property(X => X.Name)
                .HasColumnType("varchar")
                .HasMaxLength(80)
                .IsRequired();

            builder.Property(X => X.Description)
                .HasColumnType("varchar")
                .HasMaxLength(200);

            builder.Property(X => X.Supplier)
                .HasColumnType("varchar")
                .HasMaxLength(80);

            builder.Property(X => X.Location)
                .HasColumnType("varchar")
                .HasMaxLength(60);

            builder.Property(X => X.UnitCost)
                .HasPrecision(12, 2);

            builder.ToTable(Tb => {
                Tb.HasCheckConstraint("InventoryQuantityCheck", "Quantity >= 0");
                Tb.HasCheckConstraint("InventoryUnitCostCheck", "UnitCost >= 0");
                Tb.HasCheckConstraint("InventoryReorderLevelCheck", "ReorderLevel Is Null Or ReorderLevel >= 0");
            });

            // The Lists Filter By Kind Before Anything Else, And The Dashboard Asks For Low Stock.
            builder.HasIndex(X => X.Kind);
        }
    }
}
