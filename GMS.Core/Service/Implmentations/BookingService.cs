using AutoMapper;
using Domin.Contract;
using Domin.GymEntities;
using Services.Abstraction.Contract;
using Shared.DTOs.BookingDTOs;
using Shared.DTOs.MembershipDTOs;

namespace Services.Implmentations {
    /// <summary>
    /// Seat Reservations Against Scheduled Classes, Plus The Weekly Timetable Built On Top Of Them.
    /// Every Booking Is Validated Against Four Rules: The Class Must Still Be In The Future, It Must
    /// Have A Free Seat, The Member Must Hold A Live Membership, And They Must Not Already Be Booked
    /// Into An Overlapping Class.
    /// </summary>
    public class BookingService(IUnitOfWork _unitOfWork, IMapper _mapper) : IBookingService {

        public async Task<WeeklyScheduleDTO> GetWeeklySchedule(DateOnly? weekStart = null) {
            var anchor = weekStart ?? DateOnly.FromDateTime(DateTime.Now);
            var start = StartOfWeek(anchor);
            var from = start.ToDateTime(TimeOnly.MinValue);
            var to = start.AddDays(7).ToDateTime(TimeOnly.MinValue);

            var sessions = (await _unitOfWork.GetSessionRepository().GetSessionsInRangeAsync(from, to)).ToList();
            var bookedCounts = await _unitOfWork.GetBookingRepository().GetBookedCountsAsync(sessions.Select(S => S.Id));

            var slots = sessions.Select(S => {
                var slot = _mapper.Map<ScheduleSlotDTO>(S);
                slot.BookedSlots = bookedCounts.TryGetValue(S.Id, out var count) ? count : 0;
                return slot;
            }).ToList();

            var schedule = new WeeklyScheduleDTO { WeekStart = start };
            for (int offset = 0; offset < 7; offset++) {
                var day = start.AddDays(offset);
                schedule.Days.Add(new ScheduleDayDTO {
                    Date = day,
                    Slots = [.. slots.Where(S => DateOnly.FromDateTime(S.StartDate) == day).OrderBy(S => S.StartDate)]
                });
            }
            return schedule;
        }

        public async Task<SessionRosterDTO?> GetSessionRoster(int sessionId) {
            if (sessionId <= 0) return null;
            var session = await _unitOfWork.GetSessionRepository().GetSessionWithTrainerAndCategoryAsync(sessionId);
            if (session is null) return null;

            var bookings = await _unitOfWork.GetBookingRepository().GetSessionBookingsAsync(sessionId);

            return new SessionRosterDTO {
                SessionId = session.Id,
                CategoryName = session.SessionCategory.CategoryName,
                Description = session.Description,
                TrainerName = session.SessionTrainer.Name,
                StartDate = session.StartDate,
                EndDate = session.EndDate,
                Capacity = session.Capacity,
                Bookings = [.. _mapper.Map<IEnumerable<BookingDTO>>(bookings)]
            };
        }

        public async Task<IEnumerable<BookingDTO>> GetMemberBookings(int memberId) {
            if (memberId <= 0) return [];
            var bookings = await _unitOfWork.GetBookingRepository().GetMemberBookingsAsync(memberId);
            return _mapper.Map<IEnumerable<BookingDTO>>(bookings);
        }

        public async Task<(bool Success, string Message)> BookSession(CreateBookingDTO createBookingDTO) {
            try {
                var bookingRepo = _unitOfWork.GetBookingRepository();

                var session = await _unitOfWork.GetSessionRepository().GetSessionWithTrainerAndCategoryAsync(createBookingDTO.SessionId);
                if (session is null) return (false, "That Session No Longer Exists.");

                var member = await _unitOfWork.GetRepository<Member>().GetAsync(createBookingDTO.MemberId);
                if (member is null) return (false, "That Member No Longer Exists.");

                if (session.StartDate <= DateTime.Now)
                    return (false, "This Class Has Already Started, So It Can No Longer Be Booked.");

                if (await bookingRepo.ExistsAsync(member.Id, session.Id))
                    return (false, $"{member.Name} Is Already Booked Into This Class.");

                var booked = await _unitOfWork.GetSessionRepository().GetCountOfBookedSlotsAsync(session.Id);
                if (booked >= session.Capacity)
                    return (false, $"This Class Is Full ({session.Capacity}/{session.Capacity} Seats Taken).");

                var membership = await _unitOfWork.GetMembershipRepository().GetActiveForMemberAsync(member.Id);
                if (membership is null)
                    return (false, $"{member.Name} Has No Active Membership. Subscribe Them To A Plan First.");
                if (membership.EndDate < session.StartDate)
                    return (false, $"{member.Name}'s Membership Ends On {membership.EndDate:MMM dd, yyyy}, Before This Class Runs.");

                if (await bookingRepo.HasClashingBookingAsync(member.Id, session.StartDate, session.EndDate))
                    return (false, $"{member.Name} Is Already Booked Into Another Class At That Time.");

                await bookingRepo.AddAsync(new MemberSession {
                    MemberId = member.Id,
                    SessionId = session.Id,
                    CreatedAt = DateOnly.FromDateTime(DateTime.Now),
                    UpdatedAt = DateOnly.FromDateTime(DateTime.Now)
                });

                var saved = await _unitOfWork.SaveChangesAsync() > 0;
                return saved
                    ? (true, $"{member.Name} Was Booked Into {session.SessionCategory.CategoryName} On {session.StartDate:MMM dd, HH:mm}.")
                    : (false, "The Booking Could Not Be Saved.");
            } catch (Exception) {
                return (false, "Booking Failed. Please Try Again.");
            }
        }

        public async Task<(bool Success, string Message)> CancelBooking(int bookingId) {
            try {
                var bookingRepo = _unitOfWork.GetBookingRepository();
                var booking = await bookingRepo.GetWithDetailsAsync(bookingId);
                if (booking is null) return (false, "That Booking No Longer Exists.");

                if (booking.Session.StartDate <= DateTime.Now)
                    return (false, "This Class Has Already Started, So Its Bookings Can No Longer Be Released.");

                var memberName = booking.Member.Name;
                bookingRepo.Delete(booking);
                var saved = await _unitOfWork.SaveChangesAsync() > 0;
                return saved
                    ? (true, $"{memberName}'s Booking Was Cancelled.")
                    : (false, "The Booking Could Not Be Cancelled.");
            } catch (Exception) {
                return (false, "Cancelling The Booking Failed.");
            }
        }

        /// <summary>
        /// Members Who Could Still Take A Seat In This Class: Subscribed, Not Already Booked,
        /// And Free At That Hour. Keeps The Booking Dropdown From Offering Choices That Would
        /// Only Be Rejected On Submit.
        /// </summary>
        public async Task<IEnumerable<MemberSelectDTO>> GetBookableMembersForSession(int sessionId) {
            var session = await _unitOfWork.GetSessionRepository().GetAsync(sessionId);
            if (session is null) return [];

            var bookingRepo = _unitOfWork.GetBookingRepository();
            var alreadyBooked = await bookingRepo.GetBookedMemberIdsAsync(sessionId);
            var activeMemberships = await _unitOfWork.GetMembershipRepository().GetActiveByMemberAsync();
            var members = await _unitOfWork.GetRepository<Member>().GetAllAsync();

            var candidates = members
                .Where(M => !alreadyBooked.Contains(M.Id))
                .Where(M => activeMemberships.TryGetValue(M.Id, out var membership) && membership.EndDate >= session.StartDate)
                .OrderBy(M => M.Name)
                .ToList();

            var bookable = new List<MemberSelectDTO>();
            foreach (var member in candidates) {
                if (await bookingRepo.HasClashingBookingAsync(member.Id, session.StartDate, session.EndDate)) continue;
                bookable.Add(new MemberSelectDTO { Id = member.Id, Name = member.Name, Email = member.Email });
            }
            return bookable;
        }

        /// <summary>Monday Of The Week The Given Date Falls In.</summary>
        private static DateOnly StartOfWeek(DateOnly date) {
            var diff = ((int)date.DayOfWeek - (int)DayOfWeek.Monday + 7) % 7;
            return date.AddDays(-diff);
        }
    }
}
