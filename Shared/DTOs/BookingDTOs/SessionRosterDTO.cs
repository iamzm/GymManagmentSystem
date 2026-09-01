namespace Shared.DTOs.BookingDTOs {
    /// <summary>Everyone Booked Into One Session, Plus The Session Header Shown Above The Roster.</summary>
    public class SessionRosterDTO {
        public int SessionId { get; set; }
        public string CategoryName { get; set; } = null!;
        public string Description { get; set; } = null!;
        public string TrainerName { get; set; } = null!;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int Capacity { get; set; }
        public List<BookingDTO> Bookings { get; set; } = [];

        public int BookedSlots => Bookings.Count;
        public int AvailableSlots => Math.Max(Capacity - BookedSlots, 0);
        public bool IsFull => AvailableSlots == 0;
        public int FillPercent => Capacity <= 0 ? 0 : Math.Clamp((int)Math.Round(BookedSlots * 100d / Capacity), 0, 100);
        public bool CanBook => StartDate > DateTime.Now && !IsFull;

        public string Status {
            get {
                if (StartDate > DateTime.Now) return "Upcoming";
                return EndDate >= DateTime.Now ? "Ongoing" : "Completed";
            }
        }
    }
}
