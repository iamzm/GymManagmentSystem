using System.ComponentModel.DataAnnotations;

namespace Domin.Enums {
    /// <summary>
    /// Roles For Staff Who Are Not Trainers. Trainers Keep Their Own Entity And Their Own
    /// Specialties List, Because They Are Tied To Sessions And Bookings In A Way No Other
    /// Role Is.
    /// </summary>
    public enum JobTitle {
        [Display(Name = "Receptionist")]
        Receptionist = 1,

        [Display(Name = "Gym Manager")]
        Manager = 2,

        [Display(Name = "Floor Supervisor")]
        FloorSupervisor = 3,

        [Display(Name = "Cleaner")]
        Cleaner = 4,

        [Display(Name = "Maintenance Technician")]
        Maintenance = 5,

        [Display(Name = "Security Guard")]
        Security = 6,

        [Display(Name = "Nutritionist")]
        Nutritionist = 7,

        [Display(Name = "Accountant")]
        Accountant = 8,

        [Display(Name = "Sales & Marketing")]
        Sales = 9,
    }
}
