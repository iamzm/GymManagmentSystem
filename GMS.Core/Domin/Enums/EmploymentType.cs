using System.ComponentModel.DataAnnotations;

namespace Domin.Enums {
    public enum EmploymentType {
        [Display(Name = "Full Time")]
        FullTime = 1,

        [Display(Name = "Part Time")]
        PartTime = 2,

        [Display(Name = "Contract")]
        Contract = 3,
    }
}
