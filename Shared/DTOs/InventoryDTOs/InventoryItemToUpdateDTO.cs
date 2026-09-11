using Domin.Enums;
using System.ComponentModel.DataAnnotations;

namespace Shared.DTOs.InventoryDTOs {
    /// <summary>Same Shape And Same Rules As Creating; Kept Separate So The Two Can Diverge.</summary>
    public class InventoryItemToUpdateDTO : CreateInventoryItemDTO {
        public int Id { get; set; }
    }
}
