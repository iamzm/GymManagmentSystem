using System.ComponentModel.DataAnnotations;

namespace Domin.Enums {
    public enum ExpenseCategory {
        [Display(Name = "Rent")]
        Rent = 1,

        [Display(Name = "Salaries & Wages")]
        Salaries = 2,

        [Display(Name = "Utilities")]
        Utilities = 3,

        [Display(Name = "Equipment Purchase")]
        Equipment = 4,

        [Display(Name = "Repairs & Maintenance")]
        Maintenance = 5,

        [Display(Name = "Marketing & Advertising")]
        Marketing = 6,

        [Display(Name = "Supplies & Consumables")]
        Supplies = 7,

        [Display(Name = "Insurance")]
        Insurance = 8,

        [Display(Name = "Taxes & Licences")]
        Taxes = 9,

        [Display(Name = "Other")]
        Other = 10,
    }
}
