namespace Shared.DTOs.MembershipDTOs {
    public class PlanSelectDTO {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public int DurationDays { get; set; }
        public decimal Price { get; set; }
        /// <summary>
        /// Deliberately Free Of A Currency Symbol: This Is Built In The Shared Project, Which Has
        /// No Access To The Configured Currency. The Form Label Beside The Dropdown Names It.
        /// </summary>
        public string DisplayName => $"{Name} — {DurationDays} days — {Price:N0}";
    }
}
