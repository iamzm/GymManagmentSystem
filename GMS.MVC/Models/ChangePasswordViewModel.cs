using System.ComponentModel.DataAnnotations;

namespace GMS.MVC.Models {
    public class ChangePasswordViewModel {
        [Required(ErrorMessage = "Your Current Password Is Required")]
        [DataType(DataType.Password)]
        [Display(Name = "Current Password")]
        public string CurrentPassword { get; set; } = null!;

        [Required(ErrorMessage = "A New Password Is Required")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Password Must Be At Least 6 Characters")]
        [DataType(DataType.Password)]
        [Display(Name = "New Password")]
        public string NewPassword { get; set; } = null!;

        [Required(ErrorMessage = "Please Confirm Your New Password")]
        [DataType(DataType.Password)]
        [Compare(nameof(NewPassword), ErrorMessage = "The Two Passwords Do Not Match")]
        [Display(Name = "Confirm New Password")]
        public string ConfirmPassword { get; set; } = null!;
    }
}
