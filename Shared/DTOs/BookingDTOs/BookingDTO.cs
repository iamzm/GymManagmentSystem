namespace Shared.DTOs.BookingDTOs {
    public class BookingDTO {
        public int Id { get; set; }
        public int MemberId { get; set; }
        public string MemberName { get; set; } = null!;
        public string MemberEmail { get; set; } = null!;
        public string? MemberPhoto { get; set; }
        public int SessionId { get; set; }
        public string SessionCategory { get; set; } = null!;
        public string TrainerName { get; set; } = null!;
        public DateTime SessionStart { get; set; }
        public DateTime SessionEnd { get; set; }
        public DateOnly BookedOn { get; set; }

        public string SessionStatus {
            get {
                if (SessionStart > DateTime.Now) return "Upcoming";
                return SessionEnd >= DateTime.Now ? "Ongoing" : "Completed";
            }
        }

        /// <summary>A Booking Can Only Be Released While The Class Has Not Started Yet.</summary>
        public bool CanCancel => SessionStart > DateTime.Now;
    }
}
