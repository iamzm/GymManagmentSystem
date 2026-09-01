using Domin.Enums;
using System.ComponentModel.DataAnnotations;

namespace Shared.DTOs.MemberDTOs {
    public class HealthRecordDTO {
        [Required(ErrorMessage = "Height Is Required")]
        [Range(50, 300, ErrorMessage = "Height Must Be Between 50 And 300")]
        [Display(Name = "Height (cm)")]
        public decimal Height { get; set; }

        [Required(ErrorMessage = "Weight Is Required")]
        [Range(10, 400, ErrorMessage = "Weight Must Be Between 10 And 400")]
        [Display(Name = "Weight (kg)")]
        public decimal Weight { get; set; }

        [Required(ErrorMessage = "BloodType Is Required")]
        [Display(Name = "Blood Type")]
        public BloodType BloodType { get; set; }

        [StringLength(500, ErrorMessage = "Note Must Be 500 Characters Or Fewer")]
        public string? Note { get; set; }

        /// <summary>Body Mass Index Derived From The Recorded Height And Weight.</summary>
        public decimal Bmi {
            get {
                if (Height <= 0) return 0;
                var metres = Height / 100m;
                return Math.Round(Weight / (metres * metres), 1);
            }
        }

        public string BmiCategory => Bmi switch {
            0 => "Unknown",
            < 18.5m => "Underweight",
            < 25m => "Normal",
            < 30m => "Overweight",
            _ => "Obese"
        };
    }
}
