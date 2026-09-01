using Shared.DTOs.BookingDTOs;
using Shared.DTOs.MembershipDTOs;

namespace Services.Abstraction.Contract {
    public interface IBookingService {
        /// <param name="weekStart">Any Date Inside The Week To Show; The Service Snaps It To Monday.</param>
        Task<WeeklyScheduleDTO> GetWeeklySchedule(DateOnly? weekStart = null);
        Task<SessionRosterDTO?> GetSessionRoster(int sessionId);
        Task<IEnumerable<BookingDTO>> GetMemberBookings(int memberId);
        Task<(bool Success, string Message)> BookSession(CreateBookingDTO createBookingDTO);
        Task<(bool Success, string Message)> CancelBooking(int bookingId);
        Task<IEnumerable<MemberSelectDTO>> GetBookableMembersForSession(int sessionId);
    }
}
