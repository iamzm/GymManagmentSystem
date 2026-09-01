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
            return await _dbContext.MemberShips
                .Include(X => X.Plan)
                .AsNoTracking()
                .Where(X => X.MemberId == memberId && X.EndDate >= now)
                .OrderByDescending(X => X.EndDate)
                .FirstOrDefaultAsync();
        }

        public async Task<Dictionary<int, MemberShip>> GetActiveByMemberAsync() {
            var now = DateTime.Now;
            var active = await _dbContext.MemberShips
                .Include(X => X.Plan)
                .AsNoTracking()
                .Where(X => X.EndDate >= now)
                .OrderByDescending(X => X.EndDate)
                .ToListAsync();

            // A Member Can Hold More Than One Live Contract After A Renewal; The One Running
            // Longest Is The One Worth Showing.
            return active
                .GroupBy(X => X.MemberId)
                .ToDictionary(G => G.Key, G => G.First());
        }
    }
}
