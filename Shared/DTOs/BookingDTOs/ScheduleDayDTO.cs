namespace Shared.DTOs.BookingDTOs {
    public class ScheduleDayDTO {
        public DateOnly Date { get; set; }
        public List<ScheduleSlotDTO> Slots { get; set; } = [];

        public bool IsToday => Date == DateOnly.FromDateTime(DateTime.Now);
        public string DayName => Date.ToString("ddd");
        public string DayNumber => Date.ToString("dd");
        public string MonthName => Date.ToString("MMM");
        public int TotalBooked => Slots.Sum(S => S.BookedSlots);
    }
}
