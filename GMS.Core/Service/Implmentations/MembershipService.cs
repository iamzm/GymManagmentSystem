using AutoMapper;
using Domin.Contract;
using Domin.GymEntities;
using Services.Abstraction.Contract;
using Shared.DTOs.MembershipDTOs;

namespace Services.Implmentations {
    /// <summary>
    /// Subscription Contracts Between A Member And A Plan. The End Date Is Never Supplied By The
    /// Caller — It Is Always Derived From The Plan's Duration, So A Contract Cannot Outlive What
    /// Was Paid For.
    /// </summary>
    public class MembershipService(IUnitOfWork _unitOfWork, IMapper _mapper) : IMembershipService {

        public async Task<IEnumerable<MembershipDTO>> GetAllMemberships(string? search = null, string? status = null) {
            var memberships = await _unitOfWork.GetMembershipRepository().GetAllWithMemberAndPlanAsync();
            var result = _mapper.Map<IEnumerable<MembershipDTO>>(memberships);

            if (!string.IsNullOrWhiteSpace(search)) {
                var term = search.Trim();
                result = result.Where(M =>
                    M.MemberName.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                    M.MemberEmail.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                    M.PlanName.Contains(term, StringComparison.OrdinalIgnoreCase));
            }

            result = status?.ToLowerInvariant() switch {
                "active" => result.Where(M => M.IsActive),
                "expired" => result.Where(M => !M.IsActive),
                "expiring" => result.Where(M => M.IsExpiringSoon),
                _ => result
            };

            return result.ToList();
        }

        public async Task<MembershipDetailsDTO?> GetMembershipById(int membershipId) {
            if (membershipId <= 0) return null;
            var membership = await _unitOfWork.GetMembershipRepository().GetWithMemberAndPlanAsync(membershipId);
            return membership is null ? null : _mapper.Map<MembershipDetailsDTO>(membership);
        }

        public async Task<CreateMembershipDTO?> GetMembershipToRenew(int membershipId) {
            var membership = await _unitOfWork.GetMembershipRepository().GetWithMemberAndPlanAsync(membershipId);
            if (membership is null) return null;

            // A Renewal Picks Up Where The Old Contract Ends, Unless It Already Lapsed.
            var resumeFrom = membership.EndDate.Date > DateTime.Now.Date ? membership.EndDate.Date : DateTime.Now.Date;

            return new CreateMembershipDTO {
                MemberId = membership.MemberId,
                PlanId = membership.PlanId,
                StartDate = DateOnly.FromDateTime(resumeFrom)
            };
        }

        public async Task<(bool Success, string Message)> CreateMembership(CreateMembershipDTO createMembershipDTO) {
            try {
                var member = await _unitOfWork.GetRepository<Member>().GetAsync(createMembershipDTO.MemberId);
                if (member is null) return (false, "The Selected Member No Longer Exists.");

                var plan = await _unitOfWork.GetPlanRepository().GetById(createMembershipDTO.PlanId);
                if (plan is null) return (false, "The Selected Plan No Longer Exists.");
                if (!plan.IsActive) return (false, $"'{plan.Name}' Is Deactivated And Cannot Be Sold.");

                var active = await _unitOfWork.GetMembershipRepository().GetActiveForMemberAsync(createMembershipDTO.MemberId);
                if (active is not null)
                    return (false, $"{member.Name} Already Has An Active Membership Until {active.EndDate:MMM dd, yyyy}. Renew It Instead.");

                var start = createMembershipDTO.StartDate.ToDateTime(TimeOnly.MinValue);
                var membership = new MemberShip {
                    MemberId = createMembershipDTO.MemberId,
                    PlanId = createMembershipDTO.PlanId,
                    CreatedAt = createMembershipDTO.StartDate,
                    UpdatedAt = DateOnly.FromDateTime(DateTime.Now),
                    EndDate = start.AddDays(plan.DurationDays),
                    PricePaid = plan.Price
                };

                await _unitOfWork.GetMembershipRepository().AddAsync(membership);
                var saved = await _unitOfWork.SaveChangesAsync() > 0;
                return saved
                    ? (true, $"{member.Name} Subscribed To '{plan.Name}' Until {membership.EndDate:MMM dd, yyyy}.")
                    : (false, "The Membership Could Not Be Saved.");
            } catch (Exception) {
                return (false, "Creating The Membership Failed. Please Check The Data And Try Again.");
            }
        }

        public async Task<(bool Success, string Message)> RenewMembership(int membershipId, CreateMembershipDTO renewMembershipDTO) {
            try {
                var repo = _unitOfWork.GetMembershipRepository();
                var existing = await repo.GetWithMemberAndPlanAsync(membershipId);
                if (existing is null) return (false, "That Membership No Longer Exists.");

                var plan = await _unitOfWork.GetPlanRepository().GetById(renewMembershipDTO.PlanId);
                if (plan is null) return (false, "The Selected Plan No Longer Exists.");
                if (!plan.IsActive) return (false, $"'{plan.Name}' Is Deactivated And Cannot Be Sold.");

                // A Renewal Is A New Contract Rather Than An Edit Of The Old One, So The
                // Subscription History Stays Intact.
                var start = renewMembershipDTO.StartDate.ToDateTime(TimeOnly.MinValue);
                var renewal = new MemberShip {
                    MemberId = existing.MemberId,
                    PlanId = plan.Id,
                    CreatedAt = renewMembershipDTO.StartDate,
                    UpdatedAt = DateOnly.FromDateTime(DateTime.Now),
                    EndDate = start.AddDays(plan.DurationDays),
                    PricePaid = plan.Price
                };

                await repo.AddAsync(renewal);
                var saved = await _unitOfWork.SaveChangesAsync() > 0;
                return saved
                    ? (true, $"Membership Renewed On '{plan.Name}' Until {renewal.EndDate:MMM dd, yyyy}.")
                    : (false, "The Renewal Could Not Be Saved.");
            } catch (Exception) {
                return (false, "Renewing The Membership Failed. Please Check The Data And Try Again.");
            }
        }

        public async Task<(bool Success, string Message)> CancelMembership(int membershipId) {
            try {
                var repo = _unitOfWork.GetMembershipRepository();
                var membership = await repo.GetAsync(membershipId);
                if (membership is null) return (false, "That Membership No Longer Exists.");

                repo.Delete(membership);
                var saved = await _unitOfWork.SaveChangesAsync() > 0;
                return saved
                    ? (true, "The Membership Was Cancelled.")
                    : (false, "The Membership Could Not Be Cancelled.");
            } catch (Exception) {
                return (false, "Cancelling The Membership Failed.");
            }
        }

        public async Task<IEnumerable<MemberSelectDTO>> GetMembersForDropdown() {
            var members = await _unitOfWork.GetRepository<Member>().GetAllAsync();
            return members
                .OrderBy(M => M.Name)
                .Select(M => new MemberSelectDTO { Id = M.Id, Name = M.Name, Email = M.Email })
                .ToList();
        }

        public async Task<IEnumerable<PlanSelectDTO>> GetActivePlansForDropdown() {
            var plans = await _unitOfWork.GetRepository<Plan>().GetAllAsync(P => P.IsActive);
            return plans
                .OrderBy(P => P.Price)
                .Select(P => new PlanSelectDTO { Id = P.Id, Name = P.Name, DurationDays = P.DurationDays, Price = P.Price })
                .ToList();
        }
    }
}
