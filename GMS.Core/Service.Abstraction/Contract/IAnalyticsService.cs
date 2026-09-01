using Shared.DTOs.AnalyticsDTOs;

namespace Services.Abstraction.Contract {
    public interface IAnalyticsService {
        Task<AnalyticDTO> GetAnalyticData();
        Task<DashboardDTO> GetDashboardData();
    }
}
