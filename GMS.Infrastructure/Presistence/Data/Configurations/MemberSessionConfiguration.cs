using Domin.GymEntities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Presistence.Data.Configurations {
    public class MemberSessionConfiguration : IEntityTypeConfiguration<MemberSession> {
        public void Configure(EntityTypeBuilder<MemberSession> builder) {

            builder.HasKey(X => X.Id);

            builder.Property(X => X.CreatedAt)
                .HasColumnName("BookingDate")
                .HasDefaultValueSql("GETDATE()");

            builder.HasOne(X => X.Member)
                .WithMany(X => X.MemberSession)
                .HasForeignKey(X => X.MemberId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(X => X.Session)
                .WithMany(X => X.SessionMembers)
                .HasForeignKey(X => X.SessionId)
                .OnDelete(DeleteBehavior.Cascade);

            // The Database Itself Refuses A Double Booking, So A Race Between Two
            // Requests Cannot Slip Past The Service-Level Check.
            builder.HasIndex(X => new { X.MemberId, X.SessionId }).IsUnique();
        }
    }
}
