namespace Shared.DTOs.BookingDTOs {
    /// <summary>One Class On The Timetable, With Enough Booking Context To Colour The Card.</summary>
    public class ScheduleSlotDTO {
        public int SessionId { get; set; }
        public string CategoryName { get; set; } = null!;
        public string Description { get; set; } = null!;
        public string TrainerName { get; set; } = null!;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int Capacity { get; set; }
        public int BookedSlots { get; set; }

        public int AvailableSlots => Math.Max(Capacity - BookedSlots, 0);
        public bool IsFull => AvailableSlots == 0;
        public string TimeRange => $"{StartDate:HH:mm} - {EndDate:HH:mm}";
        public double DurationMinutes => (EndDate - StartDate).TotalMinutes;

        public int FillPercent => Capacity <= 0 ? 0 : Math.Clamp((int)Math.Round(BookedSlots * 100d / Capacity), 0, 100);

        public string Status {
            get {
                if (StartDate > DateTime.Now) return "Upcoming";
                return EndDate >= DateTime.Now ? "Ongoing" : "Completed";
            }
        }
    }
}
