using System.ComponentModel.DataAnnotations;

namespace Shared.DTOs.MembershipDTOs {
    public class ChangePlanDTO {
        [Required(ErrorMessage = "Please Choose A Plan")]
        [Range(1, int.MaxValue, ErrorMessage = "Please Choose A Plan")]
        [Display(Name = "New Plan")]
        public int PlanId { get; set; }
    }
}
