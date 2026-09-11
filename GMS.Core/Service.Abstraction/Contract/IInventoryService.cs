using Shared.DTOs.InventoryDTOs;

namespace Services.Abstraction.Contract {
    public interface IInventoryService {
        Task<IEnumerable<InventoryItemDTO>> GetAllItems(string? search = null, int? kind = null, string? flag = null);
        Task<InventoryItemDTO?> GetItemDetails(int itemId);
        Task<bool> CreateItem(CreateInventoryItemDTO createdItem);
        Task<InventoryItemToUpdateDTO?> GetItemToUpdate(int itemId);
        Task<bool> UpdateItem(InventoryItemToUpdateDTO updatedItem, int itemId);
        Task<bool> RemoveItem(int itemId);

        Task<InventorySummaryDTO> GetSummary();

        /// <summary>Consumables At Or Below Their Reorder Level, Worst First.</summary>
        Task<IEnumerable<InventoryItemDTO>> GetLowStock(int take = 5);

        /// <summary>Equipment Whose Service Date Has Passed Or Is About To, Soonest First.</summary>
        Task<IEnumerable<InventoryItemDTO>> GetServiceDue(int take = 5);

        /// <summary>Adjusts Stock By A Signed Delta. Refuses To Take A Count Below Zero.</summary>
        Task<bool> AdjustQuantity(int itemId, int delta);
    }
}
