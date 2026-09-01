using Domin.Entities;
using Domin.Enums;

namespace Domin.GymEntities {
    public class Trainer : GymUser {
        public Specialties Specialties { get; set; }
        public string? Photo { get; set; }
        // HireDate == CreatedAt Of BaseEntity

        // Realationship Trainer - Sessions
        public ICollection<Session> TrainerSessions { get; set; } = null!;
    }
}
