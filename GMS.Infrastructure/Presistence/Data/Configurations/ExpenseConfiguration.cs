using Domin.GymEntities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Presistence.Data.Configurations {
    public class ExpenseConfiguration : IEntityTypeConfiguration<Expense> {
        public void Configure(EntityTypeBuilder<Expense> builder) {

            builder.Property(X => X.Title)
                .HasColumnType("varchar")
                .HasMaxLength(100)
                .IsRequired();

            builder.Property(X => X.Vendor)
                .HasColumnType("varchar")
                .HasMaxLength(80);

            builder.Property(X => X.Reference)
                .HasColumnType("varchar")
                .HasMaxLength(50);

            builder.Property(X => X.Notes)
                .HasColumnType("varchar")
                .HasMaxLength(250);

            builder.Property(X => X.Amount)
                .HasPrecision(12, 2);

            builder.ToTable(Tb => {
                Tb.HasCheckConstraint("ExpenseAmountCheck", "Amount > 0");
            });

            // Every Monthly Total And The Profit Panel Group By This Column.
            builder.HasIndex(X => X.SpentOn);
        }
    }
}
