namespace Shared.DTOs.MembershipDTOs {
    public class MembershipDetailsDTO : MembershipDTO {
        public string MemberPhone { get; set; } = null!;
        public string PlanDescription { get; set; } = null!;
        public int PlanDurationDays { get; set; }
        public decimal PlanCurrentPrice { get; set; }

        /// <summary>How Far Through The Contract The Member Is, For The Progress Bar.</summary>
        public int ProgressPercent {
            get {
                if (PlanDurationDays <= 0) return 0;
                var elapsed = (DateTime.Now.Date - StartDate.ToDateTime(TimeOnly.MinValue).Date).Days;
                var percent = (int)Math.Round(elapsed * 100d / PlanDurationDays);
                return Math.Clamp(percent, 0, 100);
            }
        }
    }
}
