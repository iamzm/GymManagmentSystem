using System.ComponentModel.DataAnnotations;

namespace GMS.MVC.Models {
    public class ForgotPasswordViewModel {
        [Required(ErrorMessage = "Email Is Required")]
        [EmailAddress(ErrorMessage = "Invalid Email Format")]
        [Display(Name = "Email Address")]
        public string Email { get; set; } = null!;
    }
}
