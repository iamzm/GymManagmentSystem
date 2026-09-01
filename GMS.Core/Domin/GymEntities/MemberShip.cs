using Domin.Entities;

namespace Domin.GymEntities {
    public class MemberShip : BaseEntity {
        // StartDate => CreatedAt Of BaseEntity
        public int MemberId { get; set; }
        public Member Member { get; set; } = null!;
        public int PlanId { get; set; }
        public Plan Plan { get; set; } = null!;
        public DateTime EndDate { get; set; }

        // The Price The Member Actually Paid When The Contract Was Signed,
        // Kept On The Contract So Later Plan Price Changes Do Not Rewrite History.
        public decimal PricePaid { get; set; }

        public string Status => EndDate >= DateTime.Now ? "Active" : "Expired";

        public int DaysRemaining {
            get {
                var days = (EndDate.Date - DateTime.Now.Date).Days;
                return days > 0 ? days : 0;
            }
        }
    }
}
