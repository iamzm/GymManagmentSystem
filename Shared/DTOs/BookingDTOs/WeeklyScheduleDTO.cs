namespace Shared.DTOs.BookingDTOs {
    /// <summary>A Seven-Day Timetable Window Plus The Cursors The View Needs To Page Through Weeks.</summary>
    public class WeeklyScheduleDTO {
        public DateOnly WeekStart { get; set; }
        public DateOnly WeekEnd => WeekStart.AddDays(6);
        public List<ScheduleDayDTO> Days { get; set; } = [];

        public DateOnly PreviousWeek => WeekStart.AddDays(-7);
        public DateOnly NextWeek => WeekStart.AddDays(7);
        public bool IsCurrentWeek {
            get {
                var today = DateOnly.FromDateTime(DateTime.Now);
                return today >= WeekStart && today <= WeekEnd;
            }
        }

        public int TotalSessions => Days.Sum(D => D.Slots.Count);
        public int TotalBookings => Days.Sum(D => D.TotalBooked);
        public int TotalCapacity => Days.Sum(D => D.Slots.Sum(S => S.Capacity));
        public string Range => $"{WeekStart:MMM dd} - {WeekEnd:MMM dd, yyyy}";
    }
}
