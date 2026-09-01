using Domin.GymEntities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Presistence.Data.Configurations {
    public class MemberShipConfiguration : IEntityTypeConfiguration<MemberShip> {
        public void Configure(EntityTypeBuilder<MemberShip> builder) {

            // A Surrogate Key Keeps Every Contract Addressable By A Single Id,
            // Which The Memberships Module Needs For Details / Renew / Cancel Routes.
            builder.HasKey(X => X.Id);

            builder.Property(X => X.CreatedAt)
                .HasColumnName("StartDate")
                .HasDefaultValueSql("GETDATE()");

            builder.Property(X => X.PricePaid)
                .HasPrecision(10, 2);

            builder.HasOne(X => X.Member)
                .WithMany(X => X.MemberShips)
                .HasForeignKey(X => X.MemberId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(X => X.Plan)
                .WithMany(X => X.PlanMembers)
                .HasForeignKey(X => X.PlanId)
                .OnDelete(DeleteBehavior.Restrict);

            // Speeds Up The "Is This Member Currently Subscribed" Lookup Used All Over The App.
            builder.HasIndex(X => new { X.MemberId, X.EndDate });

            builder.Ignore(X => X.Status);
            builder.Ignore(X => X.DaysRemaining);
        }
    }
}
