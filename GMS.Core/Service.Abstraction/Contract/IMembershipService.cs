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
    }
}
