using System.ComponentModel.DataAnnotations;

namespace Domin.Enums {
    /// <summary>Applies To Equipment Only; Consumables Leave It Unset.</summary>
    public enum ItemCondition {
        [Display(Name = "New")]
        New = 1,

        [Display(Name = "Good")]
        Good = 2,

        [Display(Name = "Needs Service")]
        NeedsService = 3,

        [Display(Name = "Out Of Service")]
        OutOfService = 4,

        [Display(Name = "Retired")]
        Retired = 5,
    }
}
