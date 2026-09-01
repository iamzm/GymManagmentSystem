using Domin.Contract;
using Domin.GymEntities;
using Microsoft.EntityFrameworkCore;
using Presistence.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Presistence.Repositories {
    internal class SessionRepository : GenericRepository<Session>, ISessionRepository {

        private readonly GymDbContext _dbContext;

        public SessionRepository(GymDbContext dbContext) : base(dbContext) {
            _dbContext = dbContext;
        }

        public async Task<IEnumerable<Session>> GetAllSessionsWithTrainerAndCategoryAsync() 
            => await _dbContext.Sessions.Include(X => X.SessionTrainer)
                                        .Include(X => X.SessionCategory)
                                        .ToListAsync();
        public async Task<Session?> GetSessionWithTrainerAndCategoryAsync(int sessionId)
            => await _dbContext.Sessions.Include(X => X.SessionTrainer)
                                            .Include(X => X.SessionCategory)
                                            .FirstOrDefaultAsync(X => X.Id == sessionId);

        public async Task<int> GetCountOfBookedSlotsAsync(int sessionId)
            => await _dbContext.MemberSessions.CountAsync(X => X.SessionId == sessionId);

        public async Task<IEnumerable<Session>> GetSessionsInRangeAsync(DateTime from, DateTime to)
            => await _dbContext.Sessions.Include(X => X.SessionTrainer)
                                        .Include(X => X.SessionCategory)
                                        .AsNoTracking()
                                        .Where(X => X.StartDate >= from && X.StartDate < to)
                                        .OrderBy(X => X.StartDate)
                                        .ToListAsync();

        public async Task<IEnumerable<Session>> GetTrainerSessionsAsync(int trainerId)
            => await _dbContext.Sessions.Include(X => X.SessionTrainer)
                                        .Include(X => X.SessionCategory)
                                        .AsNoTracking()
                                        .Where(X => X.TrainerId == trainerId)
                                        .OrderByDescending(X => X.StartDate)
                                        .ToListAsync();
    }
}
