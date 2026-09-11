using Domin.Enums;
using System.ComponentModel.DataAnnotations;

namespace Shared.DTOs.EmployeeDTOs {
    public class EmployeeToUpdateDTO {
        public string Name { get; set; } = null!;

        [Required(ErrorMessage = "Email Is Required")]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        public string Email { get; set; } = null!;

        [Required(ErrorMessage = "Phone Number Is Required")]
        [Phone(ErrorMessage = "Invalid phone number")]
        [RegularExpression(@"^03\d{9}$", ErrorMessage = "Phone Must Be A Valid Pakistani Mobile Number, e.g. 03001234567")]
        public string Phone { get; set; } = null!;

        [Required(ErrorMessage = "Building Number Is Required")]
        [Range(1, int.MaxValue, ErrorMessage = "Building Number must be greater than 0")]
        public int BuildingNumber { get; set; }

        [Required(ErrorMessage = "City Is Required")]
        [StringLength(100, MinimumLength = 2)]
        public string City { get; set; } = null!;

        [Required(ErrorMessage = "Street Is Required")]
        [StringLength(150, MinimumLength = 2)]
        public string Street { get; set; } = null!;

        [Required]
        [EnumDataType(typeof(JobTitle))]
        [Display(Name = "Job Title")]
        public JobTitle JobTitle { get; set; }

        [Required]
        [EnumDataType(typeof(EmploymentType))]
        [Display(Name = "Employment Type")]
        public EmploymentType EmploymentType { get; set; }

        [Range(0, 100000000, ErrorMessage = "Salary Must Be Zero Or More")]
        [Display(Name = "Monthly Salary")]
        public decimal MonthlySalary { get; set; }

        [Display(Name = "Currently Employed")]
        public bool IsActive { get; set; }

        public string? Photo { get; set; }
    }
}
