using Shared.DTOs.MembershipDTOs;

namespace Services.Abstraction.Contract {
    public interface IMembershipService {
        /// <param name="status">"active", "expired", "expiring" Or Null For All.</param>
        Task<IEnumerable<MembershipDTO>> GetAllMemberships(string? search = null, string? status = null);
        Task<MembershipDetailsDTO?> GetMembershipById(int membershipId);
        Task<CreateMembershipDTO?> GetMembershipToRenew(int membershipId);
        Task<(bool Success, string Message)> CreateMembership(CreateMembershipDTO createMembershipDTO);
        Task<(bool Success, string Message)> RenewMembership(int membershipId, CreateMembershipDTO renewMembershipDTO);
        Task<(bool Success, string Message)> CancelMembership(int membershipId);
        Task<IEnumerable<MemberSelectDTO>> GetMembersForDropdown();
        Task<IEnumerable<PlanSelectDTO>> GetActivePlansForDropdown();

        /// <summary>The Member's Own Plan Screen: Current Term, Any Booked Change, And The Options.</summary>
        Task<MyMembershipDTO?> GetMyMembership(int memberId);

        /// <summary>
        /// Books A Plan Change To Begin When The Current Term Runs Out, So The Member Keeps
        /// Everything They Have Already Paid For. Replaces Any Change Already Booked.
        /// </summary>
        Task<(bool Success, string Message)> SchedulePlanChange(int memberId, int planId);

        /// <summary>Drops A Booked Change, Leaving The Member On Their Current Plan.</summary>
        Task<(bool Success, string Message)> CancelScheduledChange(int memberId);
    }
}
