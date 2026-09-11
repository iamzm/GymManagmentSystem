namespace Shared.DTOs.InventoryDTOs {
    /// <summary>The Figures Along The Top Of The Inventory Page.</summary>
    public class InventorySummaryDTO {
        public int EquipmentCount { get; set; }
        public int ConsumableCount { get; set; }
        public decimal TotalValue { get; set; }
        public int LowStockCount { get; set; }
        public int ServiceDueCount { get; set; }
    }
}
