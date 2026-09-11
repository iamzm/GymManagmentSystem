using Domin.Entities;
using Domin.Enums;

namespace Domin.GymEntities {
    /// <summary>
    /// Anything The Gym Owns Or Stocks. One Table Covers Both Kinds — See ItemKind For Why —
    /// With The Kind-Specific Columns Left Null For The Kind They Do Not Apply To.
    /// </summary>
    public class InventoryItem : BaseEntity {
        public string Name { get; set; } = null!;
        public string? Description { get; set; }
        public ItemKind Kind { get; set; }

        public int Quantity { get; set; }

        /// <summary>Cost Of One Unit, So Quantity * UnitCost Values The Holding.</summary>
        public decimal UnitCost { get; set; }

        public string? Supplier { get; set; }
        public DateOnly? PurchasedOn { get; set; }

        // --- Consumables ---
        /// <summary>Stock At Or Below This Is Flagged As Running Low. Null For Equipment.</summary>
        public int? ReorderLevel { get; set; }

        // --- Equipment ---
        /// <summary>Where In The Gym It Sits, So It Can Actually Be Found. Null For Consumables.</summary>
        public string? Location { get; set; }
        public ItemCondition? Condition { get; set; }

        /// <summary>Next Service Due. A Date In The Past Is Overdue And Surfaces On The Dashboard.</summary>
        public DateOnly? NextServiceOn { get; set; }
    }
}
