namespace Shared.DTOs.MembershipDTOs {
    /// <summary>One Plan A Member Could Move To, With The Change Already Worked Out.</summary>
    public class PlanOptionDTO {
        public int PlanId { get; set; }
        public string Name { get; set; } = null!;
        public string Description { get; set; } = null!;
        public int DurationDays { get; set; }
        public decimal Price { get; set; }

        /// <summary>What The Member Pays Today, For Comparison.</summary>
        public decimal CurrentPrice { get; set; }
        public bool IsCurrentPlan { get; set; }

        /// <summary>The Day This Plan Would Take Over — The End Of The Term Already Paid For.</summary>
        public DateOnly EffectiveFrom { get; set; }
        public DateOnly EffectiveUntil => EffectiveFrom.AddDays(DurationDays);

        public decimal PriceDifference => Price - CurrentPrice;

        public string Direction => IsCurrentPlan ? "Current"
            : PriceDifference > 0 ? "Upgrade"
            : PriceDifference < 0 ? "Downgrade"
            : "Switch";

        /// <summary>True Once The Member Has Booked This Plan As Their Next One.</summary>
        public bool IsScheduled { get; set; }
    }
}
