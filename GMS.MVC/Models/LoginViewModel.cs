using System.ComponentModel.DataAnnotations;

namespace GMS.MVC.Models {
    public class LoginViewModel {
        [Required(ErrorMessage = "Email Is Required")]
        [EmailAddress(ErrorMessage = "Invalid Email Format")]
        [Display(Name = "Email Address")]
        public string Email { get; set; } = null!;

        [Required(ErrorMessage = "Password Is Required")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = null!;

        [Display(Name = "Keep Me Signed In")]
        public bool RememberMe { get; set; }

        public string? ReturnUrl { get; set; }
    }
}
