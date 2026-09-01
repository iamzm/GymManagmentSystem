namespace Shared.DTOs.SessionDTOs {
    public class SessionDTO {
        public int Id { get; set; }
        public string CategoryName { get; set; } = null!;
        public string Description { get; set; } = null!;
        public int TrainerId { get; set; }
        public string TrainerName { get; set; } = null!;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int Capacity { get; set; }
        public int AvailableSlots { get; set; }

        // Computed Properties
        public int BookedSlots => Math.Max(Capacity - AvailableSlots, 0);
        public bool IsFull => AvailableSlots <= 0;
        public int FillPercent => Capacity <= 0 ? 0 : Math.Clamp((int)Math.Round(BookedSlots * 100d / Capacity), 0, 100);
        public string DateDisplay => StartDate.ToString("MMM dd, yyyy");
        public string DayDisplay => StartDate.ToString("dddd");
        public string TimeRangeDisplay => $"{StartDate:hh:mm tt} - {EndDate:hh:mm tt}";
        public TimeSpan Duration => EndDate - StartDate;
        public string DurationDisplay {
            get {
                var duration = Duration;
                if (duration.TotalMinutes < 60) return $"{(int)duration.TotalMinutes} min";
                return duration.Minutes == 0
                    ? $"{(int)duration.TotalHours} hr"
                    : $"{(int)duration.TotalHours} hr {duration.Minutes} min";
            }
        }

        public string Status {
            get {
                if (StartDate > DateTime.Now) return "Upcoming";
                return EndDate >= DateTime.Now ? "Ongoing" : "Completed";
            }
        }

        /// <summary>A Class Is Only Editable While It Has Not Started And Nobody Has Booked It.</summary>
        public bool CanModify => StartDate > DateTime.Now && BookedSlots == 0;
    }
}
