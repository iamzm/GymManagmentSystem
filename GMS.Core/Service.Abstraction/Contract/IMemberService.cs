using Shared.DTOs.BookingDTOs;
using Shared.DTOs.MemberDTOs;
using Shared.DTOs.MembershipDTOs;

namespace Services.Abstraction.Contract {
    public interface IMemberService {
        Task<IEnumerable<MemberDTO>> GetAllMembers(string? search = null, string? status = null);
        Task<bool> CreateMember(CreateMemberDTO createMemberDTO);
        Task<MemberDetailsDTO?> GetMemberDetailsById(int memberId);
        Task<HealthRecordDTO?> GetMemberHealthRecordDTO(int memberId);
        Task<MemberToUpdateDTO?> GetMemberToUpdate(int memberId);
        Task<bool> UpdateMemberDetails(int memberId, MemberToUpdateDTO memberToUpdateDTO);
        Task<bool> RemoveMember(int memberId);

        /// <summary>The Member's Subscription History, Newest First.</summary>
        Task<IEnumerable<MembershipDTO>> GetMemberMemberships(int memberId);
        Task<IEnumerable<BookingDTO>> GetMemberBookings(int memberId);
        /// <summary>The Stored Photo File Name, So A Replaced Photo Can Be Deleted From Disk.</summary>
        Task<string?> GetMemberPhoto(int memberId);
    }
}
