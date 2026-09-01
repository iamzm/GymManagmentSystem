using Domin.Enums;
using System.ComponentModel.DataAnnotations;

namespace Shared.DTOs.MemberDTOs {
    public class CreateMemberDTO {
        [Required(ErrorMessage = "Name Is Required")]
        [StringLength(50, MinimumLength = 2, ErrorMessage = "Name Must Be Between 2 And 50 Char")]
        [RegularExpression(@"^[a-zA-Z\s]+$", ErrorMessage = "Name Can Be Contain Only Letters And Spaces")]
        public string Name { get; set; } = null!;

        [Required(ErrorMessage = "Email Is Required")]
        [EmailAddress(ErrorMessage = "Invalid Email Format")]
        [DataType(DataType.EmailAddress)] // UI Hint
        [StringLength(100, MinimumLength = 5, ErrorMessage = "Email Must Be Between 2 And 50 Char")]
        public string Email { get; set; } = null!;

        [Required(ErrorMessage = "Phone Is Required")]
        [Phone(ErrorMessage = "Invalid Phone Format")]
        [RegularExpression(@"^(011|012|010|015)\d{8}", ErrorMessage = "Phone Must Be Valid Egyptaion Number")]
        public string Phone { get; set; } = null!;

        [Required(ErrorMessage = "DateOfBirth Is Required")]
        [DataType(DataType.Date)]
        public DateOnly DateOfBirth { get; set; }

        [Required(ErrorMessage = "Gender Is Required")]
        public Gender Gender { get; set; }

        [Required(ErrorMessage = "Building Number Is Required")]
        [Range(1, 9000, ErrorMessage = "Building Number Must Be Between 1 And 9000")]
        public int BuildingNumber { get; set; }

        [Required(ErrorMessage = "Street Is Required")]
        [StringLength(30, MinimumLength = 2, ErrorMessage = "Street Must Be Between 2 And 30")]
        public string Street { get; set; } = null!;

        [Required(ErrorMessage = "City Is Required")]
        [StringLength(30, MinimumLength = 2, ErrorMessage = "City Must Be Between 2 And 50 Char")]
        [RegularExpression(@"^[a-zA-Z\s]+$", ErrorMessage = "City Can Be Contain Only Letters And Spaces")]
        public string City { get; set; } = null!;

        /// <summary>File Name Of The Uploaded Profile Photo, Set By The Controller After The
        /// Attachment Service Has Stored It. Not Posted Directly By The Browser.</summary>
        public string? Photo { get; set; }

        [Required(ErrorMessage = "Health Record Is Required")]
        public HealthRecordDTO HealthRecordDTO { get; set; } = null!;
    }
}
