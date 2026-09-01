using Domin.Contract;
using Domin.GymEntities;
using Microsoft.EntityFrameworkCore;
using Presistence.Data;

namespace Presistence.Repositories {
    internal class BookingRepository : GenericRepository<MemberSession>, IBookingRepository {

        private readonly GymDbContext _dbContext;

        public BookingRepository(GymDbContext dbContext) : base(dbContext) {
            _dbContext = dbContext;
        }

        public async Task<IEnumerable<MemberSession>> GetSessionBookingsAsync(int sessionId)
            => await _dbContext.MemberSessions
                .Include(X => X.Member)
                .Include(X => X.Session).ThenInclude(S => S.SessionTrainer)
                .Include(X => X.Session).ThenInclude(S => S.SessionCategory)
                .AsNoTracking()
                .Where(X => X.SessionId == sessionId)
                .OrderBy(X => X.Member.Name)
                .ToListAsync();

        public async Task<IEnumerable<MemberSession>> GetMemberBookingsAsync(int memberId)
            => await _dbContext.MemberSessions
                .Include(X => X.Member)
                .Include(X => X.Session).ThenInclude(S => S.SessionTrainer)
                .Include(X => X.Session).ThenInclude(S => S.SessionCategory)
                .AsNoTracking()
                .Where(X => X.MemberId == memberId)
                .OrderByDescending(X => X.Session.StartDate)
                .ToListAsync();

        public async Task<MemberSession?> GetWithDetailsAsync(int bookingId)
            => await _dbContext.MemberSessions
                .Include(X => X.Member)
                .Include(X => X.Session).ThenInclude(S => S.SessionTrainer)
                .Include(X => X.Session).ThenInclude(S => S.SessionCategory)
                .FirstOrDefaultAsync(X => X.Id == bookingId);

        public async Task<bool> ExistsAsync(int memberId, int sessionId)
            => await _dbContext.MemberSessions.AnyAsync(X => X.MemberId == memberId && X.SessionId == sessionId);

        public async Task<Dictionary<int, int>> GetBookedCountsAsync(IEnumerable<int> sessionIds) {
            var ids = sessionIds.Distinct().ToList();
            if (ids.Count == 0) return [];
            return await _dbContext.MemberSessions
                .Where(X => ids.Contains(X.SessionId))
                .GroupBy(X => X.SessionId)
                .Select(G => new { SessionId = G.Key, Count = G.Count() })
                .ToDictionaryAsync(G => G.SessionId, G => G.Count);
        }

        public async Task<HashSet<int>> GetBookedMemberIdsAsync(int sessionId) {
            var ids = await _dbContext.MemberSessions
                .Where(X => X.SessionId == sessionId)
                .Select(X => X.MemberId)
                .ToListAsync();
            return [.. ids];
        }

        public async Task<bool> HasClashingBookingAsync(int memberId, DateTime start, DateTime end)
            // Two Windows Overlap Unless One Finishes Before The Other Begins.
            => await _dbContext.MemberSessions
                .AnyAsync(X => X.MemberId == memberId
                            && X.Session.StartDate < end
                            && start < X.Session.EndDate);
    }
}
