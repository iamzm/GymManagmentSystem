using Domin.Entities;

namespace Domin.GymEntities {
    public class Member : GymUser {
        public string? Photo { get; set; }
        // JoinDate == CreatedAt Of BaseEntity

        // Relationships Member - HeathRecord
        public HealthRecord HealthRecord { get; set; } = null!;

        // Relationships Member - MemberSession
        public ICollection<MemberSession> MemberSession { get; set; } = null!;

        // Relationships Member - MemberShip
        public ICollection<MemberShip> MemberShips { get; set; } = null!;
    }
}
