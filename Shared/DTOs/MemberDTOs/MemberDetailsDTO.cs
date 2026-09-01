namespace Shared.DTOs.MemberDTOs {
    public class MemberDetailsDTO {
        public int Id { get; set; }
        public string? Photo { get; set; }
        public string Name { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string Phone { get; set; } = null!;
        public string Gender { get; set; } = null!;
        public string? PlanName { get; set; }
        public string? DateOfBirth { get; set; }
        public string? MemberShipStartDate { get; set; }
        public string? MemberShipEndDate { get; set; }
        public string? Address { get; set; }
        public DateOnly JoinedOn { get; set; }
        public HealthRecordDTO? HealthRecord { get; set; }
        public int TotalBookings { get; set; }
        public int TotalMemberships { get; set; }

        public bool HasActivePlan => PlanName is not null;

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
