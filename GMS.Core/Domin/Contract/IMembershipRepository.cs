using Domin.GymEntities;

namespace Domin.Contract {
    public interface IMembershipRepository : IGenericRepository<MemberShip> {
        /// <summary>Contracts With Their Member And Plan Eagerly Loaded, Newest First.</summary>
        Task<IEnumerable<MemberShip>> GetAllWithMemberAndPlanAsync();
        Task<MemberShip?> GetWithMemberAndPlanAsync(int membershipId);
        Task<IEnumerable<MemberShip>> GetByMemberAsync(int memberId);

        /// <summary>
        /// The Contract Running Right Now — Already Started And Not Yet Ended. A Contract Dated To
        /// Begin Later Is Deliberately Excluded: It Is The Member's Next Plan, Not Their Current One.
        /// </summary>
        Task<MemberShip?> GetActiveForMemberAsync(int memberId);

        /// <summary>
        /// The Member's Next Contract, When One Has Been Booked To Start After Today — An Upgrade
        /// Or Downgrade Waiting For The Current Term To Run Out.
        /// </summary>
        Task<MemberShip?> GetScheduledForMemberAsync(int memberId);

        /// <summary>One Running Contract Per Member, Keyed By Member Id — Lets A List Screen Show
        /// Subscription State For Every Row Without A Query Per Row.</summary>
        Task<Dictionary<int, MemberShip>> GetActiveByMemberAsync();

        /// <summary>
        /// Whether The Member Holds A Contract Covering The Given Moment. Used By Booking, Where A
        /// Class Later This Year May Fall Under A Contract That Has Not Started Yet.
        /// </summary>
        Task<bool> HasCoverAtAsync(int memberId, DateTime moment);
    }
}
