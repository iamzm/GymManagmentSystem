namespace Shared.DTOs.InventoryDTOs {
    public class InventoryItemDTO {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public string? Description { get; set; }
        public string Kind { get; set; } = null!;
        public bool IsEquipment { get; set; }

        public int Quantity { get; set; }
        public decimal UnitCost { get; set; }
        public string? Supplier { get; set; }
        public DateOnly? PurchasedOn { get; set; }

        public int? ReorderLevel { get; set; }
        public string? Location { get; set; }
        public string? Condition { get; set; }
        public DateOnly? NextServiceOn { get; set; }

        /// <summary>What The Holding Is Worth, Which Is The Number An Owner Actually Wants.</summary>
        public decimal TotalValue => Quantity * UnitCost;

        /// <summary>Consumables Only. Equipment Has No Reorder Level And Never Reports Low.</summary>
        public bool IsLowStock => ReorderLevel.HasValue && Quantity <= ReorderLevel.Value;

        /// <summary>Equipment Only. True Once The Service Date Has Passed.</summary>
        public bool IsServiceOverdue
            => NextServiceOn.HasValue && NextServiceOn.Value < DateOnly.FromDateTime(DateTime.Now);

        /// <summary>Due Within The Next Fortnight, So It Can Be Flagged Before It Slips.</summary>
        public bool IsServiceDueSoon
            => NextServiceOn.HasValue
               && !IsServiceOverdue
               && NextServiceOn.Value <= DateOnly.FromDateTime(DateTime.Now.AddDays(14));
    }
}
