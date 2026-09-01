using Shared.DTOs.BookingDTOs;
using Shared.DTOs.MembershipDTOs;

namespace Shared.DTOs.AnalyticsDTOs {
    /// <summary>Everything The Admin Dashboard Renders In One Round Trip.</summary>
    public class DashboardDTO {
        public AnalyticDTO Stats { get; set; } = new();

        /// <summary>How Many Members Sit On Each Plan, For The Distribution Chart.</summary>
        public List<PlanBreakdownDTO> PlanBreakdown { get; set; } = [];

        /// <summary>Sessions Booked Per Day Over The Coming Week, For The Activity Chart.</summary>
        public List<TrendPointDTO> BookingTrend { get; set; } = [];

        public List<MembershipDTO> ExpiringMemberships { get; set; } = [];
        public List<ScheduleSlotDTO> NextSessions { get; set; } = [];
        public List<RecentMemberDTO> RecentMembers { get; set; } = [];
    }

    public class PlanBreakdownDTO {
        public string PlanName { get; set; } = null!;
        public int MemberCount { get; set; }
        public decimal Revenue { get; set; }
        public int Percent { get; set; }
    }

    public class TrendPointDTO {
        public string Label { get; set; } = null!;
        public int Value { get; set; }
    }

    public class RecentMemberDTO {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string? Photo { get; set; }
        public DateOnly JoinedOn { get; set; }
        public string? PlanName { get; set; }
    }
}
