using AutoMapper;
using Domin.Contract;
using Domin.GymEntities;
using Services.Abstraction.Contract;
using Shared.DTOs.AnalyticsDTOs;
using Shared.DTOs.BookingDTOs;
using Shared.DTOs.MembershipDTOs;

namespace Services.Implmentations {
    public class AnalyticsService(IUnitOfWork _unitOfWork, IMapper _mapper) : IAnalyticsService {

        public async Task<AnalyticDTO> GetAnalyticData() {
            var now = DateTime.Now;
            var monthStart = new DateOnly(now.Year, now.Month, 1);

            var memberships = (await _unitOfWork.GetRepository<MemberShip>().GetAllAsync()).ToList();
            var members = (await _unitOfWork.GetRepository<Member>().GetAllAsync()).ToList();
            var trainers = (await _unitOfWork.GetRepository<Trainer>().GetAllAsync()).ToList();
            var sessions = (await _unitOfWork.GetRepository<Session>().GetAllAsync()).ToList();
            var bookings = (await _unitOfWork.GetRepository<MemberSession>().GetAllAsync()).ToList();
            var plans = (await _unitOfWork.GetRepository<Plan>().GetAllAsync()).ToList();

            // A Contract Dated To Start Later Is Sold But Not Yet Running, So It Counts As
            // Scheduled Revenue Rather Than An Active Subscription.
            var today = DateOnly.FromDateTime(now);
            var active = memberships.Where(M => M.CreatedAt <= today && M.EndDate >= now).ToList();
            var scheduled = memberships.Where(M => M.CreatedAt > today).ToList();

            return new AnalyticDTO {
                // "Active Members" Counts People, Not Contracts — A Renewal Must Not Count Twice.
                ActiveMembers = active.Select(M => M.MemberId).Distinct().Count(),
                TotalMembers = members.Count,
                TotalTrainers = trainers.Count,
                NewMembersThisMonth = members.Count(M => M.CreatedAt >= monthStart),

                UpcomingSessions = sessions.Count(S => S.StartDate > now),
                OngoingSessions = sessions.Count(S => S.StartDate <= now && S.EndDate >= now),
                CompletedSessions = sessions.Count(S => S.EndDate < now),
                TotalBookings = bookings.Count,

                ActiveMemberships = active.Count,
                ScheduledMemberships = scheduled.Count,
                ExpiredMemberships = memberships.Count - active.Count - scheduled.Count,
                ExpiringSoon = active.Count(M => (M.EndDate.Date - now.Date).Days <= 7),
                ActivePlans = plans.Count(P => P.IsActive),

                TotalRevenue = memberships.Sum(M => M.PricePaid),
                RevenueThisMonth = memberships.Where(M => M.CreatedAt >= monthStart).Sum(M => M.PricePaid)
            };
        }

        public async Task<DashboardDTO> GetDashboardData() {
            var now = DateTime.Now;
            var dashboard = new DashboardDTO { Stats = await GetAnalyticData() };

            var membershipRepo = _unitOfWork.GetMembershipRepository();
            var today = DateOnly.FromDateTime(now);
            var allMemberships = (await membershipRepo.GetAllWithMemberAndPlanAsync()).ToList();
            var activeMemberships = allMemberships.Where(M => M.CreatedAt <= today && M.EndDate >= now).ToList();

            // Plan Distribution — Share Of Live Contracts Sitting On Each Plan.
            var activeCount = activeMemberships.Count;
            dashboard.PlanBreakdown = [.. activeMemberships
                .GroupBy(M => M.Plan.Name)
                .Select(G => new PlanBreakdownDTO {
                    PlanName = G.Key,
                    MemberCount = G.Count(),
                    Revenue = G.Sum(M => M.PricePaid),
                    Percent = activeCount == 0 ? 0 : (int)Math.Round(G.Count() * 100d / activeCount)
                })
                .OrderByDescending(P => P.MemberCount)];

            // Contracts About To Lapse, Soonest First — The Dashboard's Action List.
            dashboard.ExpiringMemberships = [.. _mapper
                .Map<IEnumerable<MembershipDTO>>(activeMemberships.OrderBy(M => M.EndDate))
                .Where(M => M.IsExpiringSoon)
                .Take(5)];

            // The Next Week Of Classes With Their Fill Levels.
            var sessions = (await _unitOfWork.GetSessionRepository()
                .GetSessionsInRangeAsync(now, now.AddDays(7))).ToList();
            var bookedCounts = await _unitOfWork.GetBookingRepository().GetBookedCountsAsync(sessions.Select(S => S.Id));

            dashboard.NextSessions = [.. sessions.Take(5).Select(S => {
                var slot = _mapper.Map<ScheduleSlotDTO>(S);
                slot.BookedSlots = bookedCounts.TryGetValue(S.Id, out var count) ? count : 0;
                return slot;
            })];

            // Bookings Per Day Across The Coming Week, For The Activity Chart.
            dashboard.BookingTrend = [.. Enumerable.Range(0, 7).Select(offset => {
                var day = DateOnly.FromDateTime(now.AddDays(offset));
                var daySessions = sessions.Where(S => DateOnly.FromDateTime(S.StartDate) == day).ToList();
                return new TrendPointDTO {
                    Label = day.ToString("ddd"),
                    Value = daySessions.Sum(S => bookedCounts.TryGetValue(S.Id, out var count) ? count : 0)
                };
            })];

            // Latest Sign-Ups With Their Current Plan, If Any.
            var activeByMember = await membershipRepo.GetActiveByMemberAsync();
            var members = await _unitOfWork.GetRepository<Member>().GetAllAsync();
            dashboard.RecentMembers = [.. members
                .OrderByDescending(M => M.CreatedAt)
                .ThenByDescending(M => M.Id)
                .Take(5)
                .Select(M => new RecentMemberDTO {
                    Id = M.Id,
                    Name = M.Name,
                    Email = M.Email,
                    Photo = M.Photo,
                    JoinedOn = M.CreatedAt,
                    PlanName = activeByMember.TryGetValue(M.Id, out var membership) ? membership.Plan?.Name : null
                })];

            return dashboard;
        }
    }
}
