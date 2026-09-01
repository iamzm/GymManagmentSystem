using Domin.Entities;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Presistence.Data.Configurations {
    public class GymUserConfiguration<T> : IEntityTypeConfiguration<T> where T : GymUser {
        public void Configure(EntityTypeBuilder<T> builder) {
            
            builder.Property(X => X.Name)
                .HasColumnType("varchar")
                .HasMaxLength(50);

            builder.Property(X => X.Email)
                .HasColumnType("varchar")
                .HasMaxLength(100);

            builder.Property(X => X.Phone)
                .HasColumnType("varchar")
                .HasMaxLength(11);

            builder.ToTable(Tb => {
                Tb.HasCheckConstraint("GymUserEmailValidCheck", "Email Like '_%@_%._%'");
                // Digits Only, Nothing About A Country. Which National Numbering Plan Applies Is
                // A Validation Rule That Belongs In The DTOs, Not Baked Into The Schema — Otherwise
                // Serving A Different Country Means A Migration.
                Tb.HasCheckConstraint("GymUserPhoneValidCheck", "Phone Not Like '%[^0-9]%' and Len(Phone) >= 10");
            });

            builder.HasIndex(X => X.Email).IsUnique();
            builder.HasIndex(X => X.Phone).IsUnique();

            // Address Configurations
            builder.OwnsOne(X => X.Address, AddressBuilder => {
                
                AddressBuilder.Property(Ab => Ab.Street)
                    .HasColumnType("varchar")
                    .HasMaxLength(30);

                AddressBuilder.Property(Ab => Ab.City)
                    .HasColumnType("varchar")
                    .HasMaxLength(30);

                AddressBuilder.Property(Ab => Ab.BuildingNumber)
                    .HasColumnName("BuildingNumber");
            });
        }
    }
}
