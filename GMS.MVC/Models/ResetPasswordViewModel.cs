using System.ComponentModel.DataAnnotations;

namespace GMS.MVC.Models {
    public class ResetPasswordViewModel {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = null!;

        /// <summary>
        /// The Single-Use Token From The Emailed Link, Base64Url-Encoded So It Survives The Round
        /// Trip Through A Query String.
        /// </summary>
        [Required]
        public string Token { get; set; } = null!;

        [Required(ErrorMessage = "A New Password Is Required")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Password Must Be At Least 6 Characters")]
        [DataType(DataType.Password)]
        [Display(Name = "New Password")]
        public string Password { get; set; } = null!;

        [Required(ErrorMessage = "Please Confirm Your New Password")]
        [DataType(DataType.Password)]
        [Compare(nameof(Password), ErrorMessage = "The Two Passwords Do Not Match")]
        [Display(Name = "Confirm New Password")]
        public string ConfirmPassword { get; set; } = null!;
    }
}
