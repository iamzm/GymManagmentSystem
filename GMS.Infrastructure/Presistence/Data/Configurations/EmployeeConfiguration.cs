using Domin.GymEntities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Presistence.Data.Configurations {
    public class EmployeeConfiguration : GymUserConfiguration<Employee>, IEntityTypeConfiguration<Employee> {
        public new void Configure(EntityTypeBuilder<Employee> builder) {

            // Same Trick Trainer Uses: The Hire Date Is BaseEntity.CreatedAt Under A Clearer Name.
            builder.Property(X => X.CreatedAt)
                .HasColumnName("HireDate")
                .HasDefaultValueSql("GETDATE()");

            builder.Property(X => X.MonthlySalary)
                .HasPrecision(12, 2);

            builder.Property(X => X.IsActive)
                .HasDefaultValue(true);

            builder.ToTable(Tb => {
                Tb.HasCheckConstraint("EmployeeSalaryCheck", "MonthlySalary >= 0");
            });

            // Must Run After The Column Rename Above, Or The Base Loses Track Of CreatedAt.
            base.Configure(builder);
        }
    }
}
