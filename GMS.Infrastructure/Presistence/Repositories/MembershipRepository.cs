using Domin.Contract;
using Domin.GymEntities;
using Microsoft.EntityFrameworkCore;
using Presistence.Data;

namespace Presistence.Repositories {
    internal class MembershipRepository : GenericRepository<MemberShip>, IMembershipRepository {

        private readonly GymDbContext _dbContext;

        public MembershipRepository(GymDbContext dbContext) : base(dbContext) {
            _dbContext = dbContext;
        }

        public async Task<IEnumerable<MemberShip>> GetAllWithMemberAndPlanAsync()
            => await _dbContext.MemberShips
                .Include(X => X.Member)
                .Include(X => X.Plan)
                .AsNoTracking()
                .OrderByDescending(X => X.CreatedAt)
                .ThenByDescending(X => X.Id)
                .ToListAsync();

        public async Task<MemberShip?> GetWithMemberAndPlanAsync(int membershipId)
            => await _dbContext.MemberShips
                .Include(X => X.Member)
                .Include(X => X.Plan)
                .AsNoTracking()
                .FirstOrDefaultAsync(X => X.Id == membershipId);

        public async Task<IEnumerable<MemberShip>> GetByMemberAsync(int memberId)
            => await _dbContext.MemberShips
                .Include(X => X.Plan)
                .Include(X => X.Member)
                .AsNoTracking()
                .Where(X => X.MemberId == memberId)
                .OrderByDescending(X => X.EndDate)
                .ToListAsync();

        public async Task<MemberShip?> GetActiveForMemberAsync(int memberId) {
            var now = DateTime.Now;
            var today = DateOnly.FromDateTime(now);
            // Started And Not Yet Finished. Ordering By The Earliest End Date Returns The Term
            // Actually Running, Rather Than A Longer One Queued Behind It.
            return await _dbContext.MemberShips
                .Include(X => X.Plan)
                .AsNoTracking()
                .Where(X => X.MemberId == memberId && X.CreatedAt <= today && X.EndDate >= now)
                .OrderBy(X => X.EndDate)
                .FirstOrDefaultAsync();
        }

        public async Task<MemberShip?> GetScheduledForMemberAsync(int memberId) {
            var today = DateOnly.FromDateTime(DateTime.Now);
            return await _dbContext.MemberShips
                .Include(X => X.Plan)
                .AsNoTracking()
                .Where(X => X.MemberId == memberId && X.CreatedAt > today)
                .OrderBy(X => X.CreatedAt)
                .FirstOrDefaultAsync();
        }

        public async Task<bool> HasCoverAtAsync(int memberId, DateTime moment) {
            var day = DateOnly.FromDateTime(moment);
            return await _dbContext.MemberShips
                .AnyAsync(X => X.MemberId == memberId && X.CreatedAt <= day && X.EndDate >= moment);
        }

        public async Task<Dictionary<int, MemberShip>> GetActiveByMemberAsync() {
            var now = DateTime.Now;
            var today = DateOnly.FromDateTime(now);
            var active = await _dbContext.MemberShips
                .Include(X => X.Plan)
                .AsNoTracking()
                .Where(X => X.CreatedAt <= today && X.EndDate >= now)
                .OrderBy(X => X.EndDate)
                .ToListAsync();

            // A Member Can Hold More Than One Running Contract After A Renewal; The One Finishing
            // Soonest Is The Term They Are Actually On.
            return active
                .GroupBy(X => X.MemberId)
                .ToDictionary(G => G.Key, G => G.First());
        }
    }
}
