namespace Shared.DTOs.TrainerDTOs {
    public class TrainerDTO {
        public int Id { get; set; }
        public string? Photo { get; set; }
        public string Name { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string Phone { get; set; } = null!;
        public string DateOfBirth { get; set; } = null!;
        public string Gender { get; set; } = null!;
        public string Address { get; set; } = null!;
        public string Specialties { get; set; } = null!;
        public DateOnly HiredOn { get; set; }

        /// <summary>Filled By The Service So The List Can Show Workload At A Glance.</summary>
        public int SessionCount { get; set; }
        public int UpcomingSessionCount { get; set; }

        public string Initials {
            get {
                var parts = Name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length == 0) return "?";
                return parts.Length == 1
                    ? parts[0][..1].ToUpperInvariant()
                    : $"{parts[0][0]}{parts[^1][0]}".ToUpperInvariant();
            }
        }
    }
}
