using Domin.Enums;
using System.ComponentModel.DataAnnotations;

namespace Shared.DTOs.InventoryDTOs {
    public class CreateInventoryItemDTO : IValidatableObject {

        [Required(ErrorMessage = "Name Is Required")]
        [StringLength(80, MinimumLength = 2, ErrorMessage = "Name must be between 2 and 80 characters")]
        public string Name { get; set; } = null!;

        [StringLength(200, ErrorMessage = "Description cannot exceed 200 characters")]
        public string? Description { get; set; }

        [Required(ErrorMessage = "Item Kind Is Required")]
        [EnumDataType(typeof(ItemKind))]
        public ItemKind Kind { get; set; } = ItemKind.Equipment;

        [Range(0, 1000000, ErrorMessage = "Quantity Must Be Zero Or More")]
        public int Quantity { get; set; }

        [Range(0, 100000000, ErrorMessage = "Unit Cost Must Be Zero Or More")]
        [Display(Name = "Unit Cost")]
        public decimal UnitCost { get; set; }

        [StringLength(80)]
        public string? Supplier { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "Purchased On")]
        public DateOnly? PurchasedOn { get; set; }

        // --- Consumables ---
        [Range(0, 1000000, ErrorMessage = "Reorder Level Must Be Zero Or More")]
        [Display(Name = "Reorder Level")]
        public int? ReorderLevel { get; set; }

        // --- Equipment ---
        [StringLength(60)]
        public string? Location { get; set; }

        public ItemCondition? Condition { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "Next Service Due")]
        public DateOnly? NextServiceOn { get; set; }

        /// <summary>
        /// The Form Shows One Set Of Fields Or The Other Depending On The Kind, So The Rules
        /// That Decide Which Are Meaningful Belong Here Rather Than In The Controller. Fields
        /// Belonging To The Other Kind Are Cleared By The Service, Not Rejected — A Stray Value
        /// From Switching The Kind Mid-Form Should Not Block A Valid Save.
        /// </summary>
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext) {
            if (Kind == ItemKind.Consumable && ReorderLevel is null)
                yield return new ValidationResult(
                    "Set A Reorder Level So Low Stock Can Be Flagged.", [nameof(ReorderLevel)]);

            if (Kind == ItemKind.Equipment && Condition is null)
                yield return new ValidationResult(
                    "Choose The Current Condition Of This Equipment.", [nameof(Condition)]);

            if (PurchasedOn.HasValue && PurchasedOn.Value > DateOnly.FromDateTime(DateTime.Now))
                yield return new ValidationResult(
                    "Purchase Date Cannot Be In The Future.", [nameof(PurchasedOn)]);
        }
    }
}
