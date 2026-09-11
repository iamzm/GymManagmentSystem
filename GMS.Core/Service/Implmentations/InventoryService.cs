using AutoMapper;
using Domin.Contract;
using Domin.Enums;
using Domin.GymEntities;
using Services.Abstraction.Contract;
using Shared.DTOs.InventoryDTOs;

namespace Services.Implmentations {
	public class InventoryService(IUnitOfWork unitOfWork, IMapper mapper) : IInventoryService {

		public async Task<IEnumerable<InventoryItemDTO>> GetAllItems(string? search = null, int? kind = null, string? flag = null) {
			var items = await unitOfWork.GetRepository<InventoryItem>().GetAllAsync();
			if (items is null || !items.Any()) return [];

			IEnumerable<InventoryItemDTO> result = mapper.Map<IEnumerable<InventoryItemDTO>>(items);

			if (!string.IsNullOrWhiteSpace(search)) {
				var term = search.Trim();
				result = result.Where(I =>
					I.Name.Contains(term, StringComparison.OrdinalIgnoreCase) ||
					(I.Supplier ?? "").Contains(term, StringComparison.OrdinalIgnoreCase) ||
					(I.Location ?? "").Contains(term, StringComparison.OrdinalIgnoreCase) ||
					(I.Description ?? "").Contains(term, StringComparison.OrdinalIgnoreCase));
			}

			if (kind is > 0) {
				var wanted = (ItemKind)kind.Value;
				result = result.Where(I => I.IsEquipment == (wanted == ItemKind.Equipment));
			}

			result = flag switch {
				"low" => result.Where(I => I.IsLowStock),
				"service" => result.Where(I => I.IsServiceOverdue || I.IsServiceDueSoon),
				_ => result,
			};

			// Whatever Needs Attention Rises To The Top; The Rest Is Alphabetical.
			return [.. result
				.OrderByDescending(I => I.IsLowStock || I.IsServiceOverdue)
				.ThenByDescending(I => I.IsServiceDueSoon)
				.ThenBy(I => I.Name)];
		}

		public async Task<InventoryItemDTO?> GetItemDetails(int itemId) {
			var item = await unitOfWork.GetRepository<InventoryItem>().GetAsync(itemId);
			return item is null ? null : mapper.Map<InventoryItemDTO>(item);
		}

		public async Task<bool> CreateItem(CreateInventoryItemDTO createdItem) {
			try {
				var item = mapper.Map<InventoryItem>(createdItem);
				Normalise(item);
				item.CreatedAt = DateOnly.FromDateTime(DateTime.Now);
				item.UpdatedAt = DateOnly.FromDateTime(DateTime.Now);

				await unitOfWork.GetRepository<InventoryItem>().AddAsync(item);
				return await unitOfWork.SaveChangesAsync() > 0;
			} catch (Exception) {
				return false;
			}
		}

		public async Task<InventoryItemToUpdateDTO?> GetItemToUpdate(int itemId) {
			var item = await unitOfWork.GetRepository<InventoryItem>().GetAsync(itemId);
			return item is null ? null : mapper.Map<InventoryItemToUpdateDTO>(item);
		}

		public async Task<bool> UpdateItem(InventoryItemToUpdateDTO updatedItem, int itemId) {
			try {
				var repo = unitOfWork.GetRepository<InventoryItem>();
				var itemToUpdate = await repo.GetAsync(itemId);
				if (itemToUpdate is null) return false;

				itemToUpdate.Name = updatedItem.Name;
				itemToUpdate.Description = updatedItem.Description;
				itemToUpdate.Kind = updatedItem.Kind;
				itemToUpdate.Quantity = updatedItem.Quantity;
				itemToUpdate.UnitCost = updatedItem.UnitCost;
				itemToUpdate.Supplier = updatedItem.Supplier;
				itemToUpdate.PurchasedOn = updatedItem.PurchasedOn;
				itemToUpdate.ReorderLevel = updatedItem.ReorderLevel;
				itemToUpdate.Location = updatedItem.Location;
				itemToUpdate.Condition = updatedItem.Condition;
				itemToUpdate.NextServiceOn = updatedItem.NextServiceOn;
				Normalise(itemToUpdate);
				itemToUpdate.UpdatedAt = DateOnly.FromDateTime(DateTime.Now);

				repo.Update(itemToUpdate);
				return await unitOfWork.SaveChangesAsync() > 0;
			} catch (Exception) {
				return false;
			}
		}

		public async Task<bool> RemoveItem(int itemId) {
			try {
				var repo = unitOfWork.GetRepository<InventoryItem>();
				var itemToRemove = await repo.GetAsync(itemId);
				if (itemToRemove is null) return false;

				repo.Delete(itemToRemove);
				return await unitOfWork.SaveChangesAsync() > 0;
			} catch (Exception) {
				return false;
			}
		}

		public async Task<InventorySummaryDTO> GetSummary() {
			var items = mapper.Map<IEnumerable<InventoryItemDTO>>(
				await unitOfWork.GetRepository<InventoryItem>().GetAllAsync()).ToList();

			return new InventorySummaryDTO {
				EquipmentCount = items.Count(I => I.IsEquipment),
				ConsumableCount = items.Count(I => !I.IsEquipment),
				TotalValue = items.Sum(I => I.TotalValue),
				LowStockCount = items.Count(I => I.IsLowStock),
				ServiceDueCount = items.Count(I => I.IsServiceOverdue || I.IsServiceDueSoon),
			};
		}

		public async Task<IEnumerable<InventoryItemDTO>> GetLowStock(int take = 5) {
			var items = mapper.Map<IEnumerable<InventoryItemDTO>>(
				await unitOfWork.GetRepository<InventoryItem>().GetAllAsync());

			// Furthest Below Its Reorder Level First, So The Most Urgent Gap Leads.
			return [.. items.Where(I => I.IsLowStock)
						   .OrderBy(I => I.Quantity - (I.ReorderLevel ?? 0))
						   .ThenBy(I => I.Name)
						   .Take(take)];
		}

		public async Task<IEnumerable<InventoryItemDTO>> GetServiceDue(int take = 5) {
			var items = mapper.Map<IEnumerable<InventoryItemDTO>>(
				await unitOfWork.GetRepository<InventoryItem>().GetAllAsync());

			return [.. items.Where(I => I.IsServiceOverdue || I.IsServiceDueSoon)
						   .OrderBy(I => I.NextServiceOn)
						   .Take(take)];
		}

		public async Task<bool> AdjustQuantity(int itemId, int delta) {
			try {
				var repo = unitOfWork.GetRepository<InventoryItem>();
				var item = await repo.GetAsync(itemId);
				if (item is null) return false;

				// A Negative Count Is Not A Real State Of The World, So Refuse Rather Than Clamp:
				// Clamping Would Silently Lose The Fact That The Books Disagree With The Shelf.
				var next = item.Quantity + delta;
				if (next < 0) return false;

				item.Quantity = next;
				item.UpdatedAt = DateOnly.FromDateTime(DateTime.Now);
				repo.Update(item);
				return await unitOfWork.SaveChangesAsync() > 0;
			} catch (Exception) {
				return false;
			}
		}

		#region Helper Methods
		/// <summary>
		/// Clears The Columns That Do Not Apply To The Chosen Kind. Switching An Item From
		/// Equipment To Consumable Mid-Form Would Otherwise Leave A Stale Service Date Behind,
		/// And The Dashboard Would Keep Reporting It As Due.
		/// </summary>
		private static void Normalise(InventoryItem item) {
			if (item.Kind == ItemKind.Equipment) {
				item.ReorderLevel = null;
			} else {
				item.Location = null;
				item.Condition = null;
				item.NextServiceOn = null;
			}
		}
		#endregion
	}
}
