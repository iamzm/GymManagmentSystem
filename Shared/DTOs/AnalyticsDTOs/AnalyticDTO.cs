namespace Shared.DTOs.AnalyticsDTOs {
    public class AnalyticDTO {
        // People
        public int TotalMembers { get; set; }
        public int ActiveMembers { get; set; }
        public int TotalTrainers { get; set; }
        public int NewMembersThisMonth { get; set; }

        // Sessions
        public int UpcomingSessions { get; set; }
        public int OngoingSessions { get; set; }
        public int CompletedSessions { get; set; }
        public int TotalBookings { get; set; }

        // Subscriptions
        public int ActiveMemberships { get; set; }
        public int ExpiredMemberships { get; set; }
        public int ExpiringSoon { get; set; }
        public int ActivePlans { get; set; }

        // Money
        public decimal TotalRevenue { get; set; }
        public decimal RevenueThisMonth { get; set; }

        public int TotalSessions => UpcomingSessions + OngoingSessions + CompletedSessions;

        /// <summary>Share Of Members Holding A Live Contract — The Headline Retention Number.</summary>
        public int ActiveMemberPercent
            => TotalMembers <= 0 ? 0 : Math.Clamp((int)Math.Round(ActiveMembers * 100d / TotalMembers), 0, 100);
    }
}
