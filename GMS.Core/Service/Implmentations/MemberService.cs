using AutoMapper;
using Domin.Contract;
using Domin.GymEntities;
using Services.Abstraction.Contract;
using Services.Specifications;
using Shared.DTOs.BookingDTOs;
using Shared.DTOs.MemberDTOs;
using Shared.DTOs.MembershipDTOs;

namespace Services.Implmentations {
    public class MemberService(IUnitOfWork _unitOfWork, IMapper _mapper) : IMemberService {

        public async Task<IEnumerable<MemberDTO>> GetAllMembers(string? search = null, string? status = null) {
            try {
                var members = await _unitOfWork.GetRepository<Member>().GetAllAsync();
                if (members is null || !members.Any()) return [];

                var result = _mapper.Map<IEnumerable<MemberDTO>>(members).ToList();

                // One Query For Every Row's Subscription State, Instead Of One Query Per Row.
                var activeByMember = await _unitOfWork.GetMembershipRepository().GetActiveByMemberAsync();
                foreach (var member in result) {
                    if (!activeByMember.TryGetValue(member.Id, out var membership)) continue;
                    member.PlanName = membership.Plan?.Name;
                    member.MembershipEndDate = membership.EndDate;
                }

                IEnumerable<MemberDTO> filtered = result;

                if (!string.IsNullOrWhiteSpace(search)) {
                    var term = search.Trim();
                    filtered = filtered.Where(M =>
                        M.Name.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                        M.Email.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                        M.Phone.Contains(term, StringComparison.OrdinalIgnoreCase));
                }

                filtered = status?.ToLowerInvariant() switch {
                    "active" => filtered.Where(M => M.IsActive),
                    "expired" => filtered.Where(M => M.MembershipEndDate is not null && !M.IsActive),
                    "noplan" => filtered.Where(M => M.MembershipEndDate is null),
                    _ => filtered
                };

                return [.. filtered.OrderBy(M => M.Name)];
            } catch (Exception) {
                return [];
            }
        }

        public async Task<MemberDetailsDTO?> GetMemberDetailsById(int memberId) {
            try {
                var member = await _unitOfWork.GetRepository<Member>()
                                              .GetAsync(new MemberWithHealthRecordSpecification(memberId));
                if (member is null) return null;

                var memberResult = _mapper.Map<MemberDetailsDTO>(member);

                var activeMemberShip = await _unitOfWork.GetMembershipRepository().GetActiveForMemberAsync(memberId);
                if (activeMemberShip is not null) {
                    memberResult.MemberShipStartDate = activeMemberShip.CreatedAt.ToString("MMM dd, yyyy");
                    memberResult.MemberShipEndDate = activeMemberShip.EndDate.ToString("MMM dd, yyyy");
                    memberResult.PlanName = activeMemberShip.Plan?.Name
                        ?? (await _unitOfWork.GetPlanRepository().GetById(activeMemberShip.PlanId))?.Name;
                }

                var memberships = await _unitOfWork.GetMembershipRepository().GetByMemberAsync(memberId);
                memberResult.TotalMemberships = memberships.Count();
                memberResult.TotalBookings = (await _unitOfWork.GetBookingRepository().GetMemberBookingsAsync(memberId)).Count();

                return memberResult;
            } catch (Exception) {
                return null;
            }
        }

        public async Task<bool> CreateMember(CreateMemberDTO createMemberDTO) {
            try {
                if (await IsEmailExist(createMemberDTO.Email) || await IsPhoneExist(createMemberDTO.Phone)) return false;
                var member = _mapper.Map<Member>(createMemberDTO);
                member.CreatedAt = DateOnly.FromDateTime(DateTime.Now);
                member.UpdatedAt = DateOnly.FromDateTime(DateTime.Now);
                await _unitOfWork.GetRepository<Member>().AddAsync(member);
                return await _unitOfWork.SaveChangesAsync() > 0;
            } catch (Exception) {
                return false;
            }
        }

        public async Task<HealthRecordDTO?> GetMemberHealthRecordDTO(int memberId) {
            try {
                // The Health Record Shares Its Primary Key With The Member It Belongs To.
                var memberHealthRecord = await _unitOfWork.GetRepository<HealthRecord>().GetAsync(memberId);
                return memberHealthRecord is null ? null : _mapper.Map<HealthRecordDTO>(memberHealthRecord);
            } catch (Exception) {
                return null;
            }
        }

        public async Task<MemberToUpdateDTO?> GetMemberToUpdate(int memberId) {
            var member = await _unitOfWork.GetRepository<Member>().GetAsync(new MemberWithHealthRecordSpecification(memberId));
            return member is null ? null : _mapper.Map<MemberToUpdateDTO>(member);
        }

        public async Task<bool> UpdateMemberDetails(int memberId, MemberToUpdateDTO memberToUpdateDTO) {
            try {
                var memberRepo = _unitOfWork.GetRepository<Member>();
                if (await IsEmailExist(memberId, memberToUpdateDTO.Email) || await IsPhoneExist(memberId, memberToUpdateDTO.Phone)) return false;

                var memberToUpdate = await memberRepo.GetAsync(new MemberWithHealthRecordSpecification(memberId));
                if (memberToUpdate is null) return false;

                memberToUpdate.Name = memberToUpdateDTO.Name;
                memberToUpdate.Email = memberToUpdateDTO.Email;
                memberToUpdate.Phone = memberToUpdateDTO.Phone;
                memberToUpdate.Photo = memberToUpdateDTO.Photo;
                memberToUpdate.Address.BuildingNumber = memberToUpdateDTO.BuildingNumber;
                memberToUpdate.Address.Street = memberToUpdateDTO.Street;
                memberToUpdate.Address.City = memberToUpdateDTO.City;
                memberToUpdate.HealthRecord.Height = memberToUpdateDTO.HealthRecordDTO.Height;
                memberToUpdate.HealthRecord.Weight = memberToUpdateDTO.HealthRecordDTO.Weight;
                memberToUpdate.HealthRecord.BloodType = memberToUpdateDTO.HealthRecordDTO.BloodType;
                memberToUpdate.HealthRecord.Note = memberToUpdateDTO.HealthRecordDTO.Note;
                memberToUpdate.UpdatedAt = DateOnly.FromDateTime(DateTime.Now);

                memberRepo.Update(memberToUpdate);
                return await _unitOfWork.SaveChangesAsync() > 0;
            } catch (Exception) {
                return false;
            }
        }

        public async Task<bool> RemoveMember(int memberId) {
            var memberRepo = _unitOfWork.GetRepository<Member>();
            var member = await memberRepo.GetAsync(new MemberWitSessionSpecification(memberId));
            if (member is null) return false;

            // A Member Booked Into A Class That Has Not Run Yet Cannot Simply Vanish.
            var activeMemberSessions = await _unitOfWork.GetRepository<MemberSession>()
                                                        .GetAllAsync(X => X.MemberId == memberId && X.Session.StartDate > DateTime.Now);
            if (activeMemberSessions.Any()) return false;

            try {
                // Memberships And Past Bookings Cascade From The Member Row Itself.
                memberRepo.Delete(member);
                return await _unitOfWork.SaveChangesAsync() > 0;
            } catch (Exception) {
                return false;
            }
        }

        public async Task<IEnumerable<MembershipDTO>> GetMemberMemberships(int memberId) {
            var memberships = await _unitOfWork.GetMembershipRepository().GetByMemberAsync(memberId);
            return _mapper.Map<IEnumerable<MembershipDTO>>(memberships);
        }

        public async Task<IEnumerable<BookingDTO>> GetMemberBookings(int memberId) {
            var bookings = await _unitOfWork.GetBookingRepository().GetMemberBookingsAsync(memberId);
            return _mapper.Map<IEnumerable<BookingDTO>>(bookings);
        }

        public async Task<string?> GetMemberPhoto(int memberId)
            => (await _unitOfWork.GetRepository<Member>().GetAsync(memberId))?.Photo;

        public async Task<int?> FindMemberIdByEmail(string email) {
            if (string.IsNullOrWhiteSpace(email)) return null;
            var matches = await _unitOfWork.GetRepository<Member>().GetAllAsync(M => M.Email == email);
            return matches.FirstOrDefault()?.Id;
        }

        #region Helper Methods
        private async Task<bool> IsEmailExist(int memberId, string email) {
            var memberEmail = await _unitOfWork.GetRepository<Member>().GetAllAsync(m => m.Email == email && m.Id != memberId);
            return memberEmail.Any();
        }
        private async Task<bool> IsEmailExist(string email) {
            var memberEmail = await _unitOfWork.GetRepository<Member>().GetAllAsync(m => m.Email == email);
            return memberEmail.Any();
        }
        private async Task<bool> IsPhoneExist(int memberId, string phone) {
            var memberPhone = await _unitOfWork.GetRepository<Member>().GetAllAsync(m => m.Phone == phone && m.Id != memberId);
            return memberPhone.Any();
        }
        private async Task<bool> IsPhoneExist(string phone) {
            var memberPhone = await _unitOfWork.GetRepository<Member>().GetAllAsync(m => m.Phone == phone);
            return memberPhone.Any();
        }
        #endregion
    }
}
