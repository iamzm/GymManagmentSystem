using Domin.Entities;

namespace Domin.Contract {
    public interface IUnitOfWork {
        Task<int> SaveChangesAsync();
        IGenericRepository<TEntity> GetRepository<TEntity>() where TEntity : BaseEntity, new();
        IPlanRepository GetPlanRepository();
        ISessionRepository GetSessionRepository();
        IMembershipRepository GetMembershipRepository();
        IBookingRepository GetBookingRepository();
    }
}
