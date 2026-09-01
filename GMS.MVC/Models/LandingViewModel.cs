using Shared.DTOs.AnalyticsDTOs;
using Shared.DTOs.BookingDTOs;
using Shared.DTOs.PlanDTOs;
using Shared.DTOs.TrainerDTOs;

namespace GMS.MVC.Models {
    /// <summary>What the public landing page shows: real gym numbers, real plans, real classes.</summary>
    public class LandingViewModel {
        public AnalyticDTO Stats { get; set; } = new();
        public List<PlanDTO> Plans { get; set; } = [];
        public List<ScheduleSlotDTO> UpcomingClasses { get; set; } = [];
        public List<TrainerDTO> Trainers { get; set; } = [];
    }
}
