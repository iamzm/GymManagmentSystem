using Domin.GymEntities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Presistence.Data.Configurations {
    public class HealthRecordConfiguration : IEntityTypeConfiguration<HealthRecord> {
        public void Configure(EntityTypeBuilder<HealthRecord> builder) {
            
            builder.ToTable("Members")
                .HasKey(X => X.Id);

            // Without An Explicit Precision EF Would Map These To decimal(18,2)'s Default And
            // Silently Truncate; A Height/Weight Only Ever Needs One Decimal Place.
            builder.Property(X => X.Height).HasPrecision(5, 1);
            builder.Property(X => X.Weight).HasPrecision(5, 1);
            
            builder.HasOne<Member>()
                .WithOne(X => X.HealthRecord)
                .HasForeignKey<HealthRecord>(X => X.Id);

            builder.Ignore(X => X.CreatedAt);
            builder.Ignore(X => X.UpdatedAt);
        }
    }
}
