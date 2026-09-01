namespace Shared.DTOs.MembershipDTOs {
    public class MembershipDTO {
        public int Id { get; set; }
        public int MemberId { get; set; }
        public string MemberName { get; set; } = null!;
        public string? MemberPhoto { get; set; }
        public string MemberEmail { get; set; } = null!;
        public int PlanId { get; set; }
        public string PlanName { get; set; } = null!;
        public decimal PricePaid { get; set; }
        public DateOnly StartDate { get; set; }
        public DateTime EndDate { get; set; }

        /// <summary>Dated To Begin Later — The Member's Next Plan, Not Their Current One.</summary>
        public bool IsScheduled => StartDate > DateOnly.FromDateTime(DateTime.Now);

        /// <summary>Running Right Now: Started, And Not Yet Finished.</summary>
        public bool IsActive => !IsScheduled && EndDate >= DateTime.Now;

        public string Status => IsScheduled ? "Scheduled" : IsActive ? "Active" : "Expired";

        /// <summary>Days Until A Scheduled Contract Takes Over.</summary>
        public int DaysUntilStart {
            get {
                var days = StartDate.DayNumber - DateOnly.FromDateTime(DateTime.Now).DayNumber;
                return days > 0 ? days : 0;
            }
        }

        public int DaysRemaining {
            get {
                var days = (EndDate.Date - DateTime.Now.Date).Days;
                return days > 0 ? days : 0;
            }
        }

        /// <summary>Active But Inside The Last Week, So The UI Can Nudge Before It Lapses.</summary>
        public bool IsExpiringSoon => IsActive && DaysRemaining <= 7;
    }
}
