using Domin.Entities;
using Domin.Enums;

namespace Domin.GymEntities {
    /// <summary>
    /// Money Going Out. Paired With Membership Revenue On The Dashboard To Give The Owner A
    /// Monthly Profit Figure Rather Than Just A Running Total Of Costs.
    /// </summary>
    public class Expense : BaseEntity {
        public string Title { get; set; } = null!;
        public ExpenseCategory Category { get; set; }
        public decimal Amount { get; set; }

        /// <summary>The Date The Money Was Actually Spent, Which Is What The Monthly Figures Use.
        /// Kept Separate From CreatedAt, Because A Bill Is Often Entered Days After It Was Paid.</summary>
        public DateOnly SpentOn { get; set; }

        public PaymentMethod PaymentMethod { get; set; }
        public string? Vendor { get; set; }

        /// <summary>Invoice Or Receipt Number, For Matching Against Paperwork.</summary>
        public string? Reference { get; set; }
        public string? Notes { get; set; }
    }
}
