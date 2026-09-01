using Domin.GymEntities;

namespace Domin.Contract {
    public interface ISessionRepository : IGenericRepository<Session> {
        Task<IEnumerable<Session>> GetAllSessionsWithTrainerAndCategoryAsync();
        Task<Session?> GetSessionWithTrainerAndCategoryAsync(int sessionId);
        Task<int> GetCountOfBookedSlotsAsync(int sessionId);

        /// <summary>Classes Starting Inside The Given Window, Ordered By Start Time — The Timetable Query.</summary>
        Task<IEnumerable<Session>> GetSessionsInRangeAsync(DateTime from, DateTime to);
        Task<IEnumerable<Session>> GetTrainerSessionsAsync(int trainerId);
    }
}
