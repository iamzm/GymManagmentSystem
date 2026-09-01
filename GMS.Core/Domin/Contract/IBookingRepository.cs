using Domin.GymEntities;

namespace Domin.Contract {
    public interface IBookingRepository : IGenericRepository<MemberSession> {
        Task<IEnumerable<MemberSession>> GetSessionBookingsAsync(int sessionId);
        Task<IEnumerable<MemberSession>> GetMemberBookingsAsync(int memberId);
        Task<MemberSession?> GetWithDetailsAsync(int bookingId);
        Task<bool> ExistsAsync(int memberId, int sessionId);

        /// <summary>Booked-Seat Counts For Many Sessions At Once, Keyed By Session Id.</summary>
        Task<Dictionary<int, int>> GetBookedCountsAsync(IEnumerable<int> sessionIds);

        /// <summary>Ids Of Members Already Booked Into The Given Session.</summary>
        Task<HashSet<int>> GetBookedMemberIdsAsync(int sessionId);

        /// <summary>True When The Member Already Has A Class That Overlaps This Time Window.</summary>
        Task<bool> HasClashingBookingAsync(int memberId, DateTime start, DateTime end);
    }
}
