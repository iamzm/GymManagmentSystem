namespace Shared.DTOs.MembershipDTOs {
    public class PlanSelectDTO {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public int DurationDays { get; set; }
        public decimal Price { get; set; }
        public string DisplayName => $"{Name} — {DurationDays} days — {Price:N0} EGP";
    }
}
