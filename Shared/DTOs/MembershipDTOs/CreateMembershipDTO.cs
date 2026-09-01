using System.ComponentModel.DataAnnotations;

namespace Shared.DTOs.MembershipDTOs {
    public class CreateMembershipDTO {
        [Required(ErrorMessage = "Member Is Required")]
        [Range(1, int.MaxValue, ErrorMessage = "Please Select A Member")]
        [Display(Name = "Member")]
        public int MemberId { get; set; }

        [Required(ErrorMessage = "Plan Is Required")]
        [Range(1, int.MaxValue, ErrorMessage = "Please Select A Plan")]
        [Display(Name = "Plan")]
        public int PlanId { get; set; }

        [Required(ErrorMessage = "Start Date Is Required")]
        [DataType(DataType.Date)]
        [Display(Name = "Start Date")]
        public DateOnly StartDate { get; set; } = DateOnly.FromDateTime(DateTime.Now);
    }
}
