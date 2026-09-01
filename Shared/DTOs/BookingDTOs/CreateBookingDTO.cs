using System.ComponentModel.DataAnnotations;

namespace Shared.DTOs.BookingDTOs {
    public class CreateBookingDTO {
        [Required(ErrorMessage = "Session Is Required")]
        [Range(1, int.MaxValue, ErrorMessage = "Please Select A Session")]
        [Display(Name = "Session")]
        public int SessionId { get; set; }

        [Required(ErrorMessage = "Member Is Required")]
        [Range(1, int.MaxValue, ErrorMessage = "Please Select A Member")]
        [Display(Name = "Member")]
        public int MemberId { get; set; }
    }
}
