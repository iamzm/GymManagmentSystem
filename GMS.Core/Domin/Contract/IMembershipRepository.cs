using Domin.GymEntities;

namespace Domin.Contract {
    public interface IMembershipRepository : IGenericRepository<MemberShip> {
        /// <summary>Contracts With Their Member And Plan Eagerly Loaded, Newest First.</summary>
        Task<IEnumerable<MemberShip>> GetAllWithMemberAndPlanAsync();
        Task<MemberShip?> GetWithMemberAndPlanAsync(int membershipId);
        Task<IEnumerable<MemberShip>> GetByMemberAsync(int memberId);

        /// <summary>The Member's Live Contract, Or Null When They Are Not Currently Subscribed.</summary>
        Task<MemberShip?> GetActiveForMemberAsync(int memberId);

        /// <summary>One Live Contract Per Member, Keyed By Member Id — Lets A List Screen Show
        /// Subscription State For Every Row Without A Query Per Row.</summary>
        Task<Dictionary<int, MemberShip>> GetActiveByMemberAsync();
    }
}
