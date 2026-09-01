using System.ComponentModel.DataAnnotations;

namespace GMS.MVC.Models {
    public class RegisterViewModel {
        [Required(ErrorMessage = "Full Name Is Required")]
        [StringLength(50, MinimumLength = 3, ErrorMessage = "Full Name Must Be Between 3 And 50 Characters")]
        [RegularExpression(@"^[a-zA-Z\s]+$", ErrorMessage = "Full Name Can Contain Only Letters And Spaces")]
        [Display(Name = "Full Name")]
        public string FullName { get; set; } = null!;

        [Required(ErrorMessage = "Email Is Required")]
        [EmailAddress(ErrorMessage = "Invalid Email Format")]
        [Display(Name = "Email Address")]
        public string Email { get; set; } = null!;

        [Required(ErrorMessage = "Password Is Required")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Password Must Be At Least 6 Characters")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = null!;

        [Required(ErrorMessage = "Please Confirm Your Password")]
        [DataType(DataType.Password)]
        [Compare(nameof(Password), ErrorMessage = "The Two Passwords Do Not Match")]
        [Display(Name = "Confirm Password")]
        public string ConfirmPassword { get; set; } = null!;

        public string? ReturnUrl { get; set; }
    }
}
