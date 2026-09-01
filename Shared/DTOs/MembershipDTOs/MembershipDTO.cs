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

        public string Status => EndDate >= DateTime.Now ? "Active" : "Expired";
        public bool IsActive => EndDate >= DateTime.Now;

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
