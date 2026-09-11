using System.ComponentModel.DataAnnotations;

namespace Domin.Enums {
    /// <summary>
    /// Equipment And Consumables Live In One Table Because They Share Most Of Their Columns —
    /// Name, Quantity, Unit Cost, Supplier. The Fields That Differ Are Nullable And The UI
    /// Shows Only The Ones That Apply: Service Dates And Condition For Equipment, Reorder
    /// Levels For Consumables.
    /// </summary>
    public enum ItemKind {
        [Display(Name = "Equipment")]
        Equipment = 1,

        [Display(Name = "Consumable Stock")]
        Consumable = 2,
    }
}
