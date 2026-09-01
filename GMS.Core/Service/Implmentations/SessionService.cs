using AutoMapper;
using Domin.Contract;
using Domin.GymEntities;
using Services.Abstraction.Contract;
using Shared.DTOs.SessionDTOs;
using Shared.DTOs.TrainerDTOs;

namespace Services.Implmentations {
    public class SessionService(IUnitOfWork _unitOfWork, IMapper _mapper) : ISessionService {
        public async Task<IEnumerable<SessionDTO>> GetAllSessions(string? search = null, string? status = null) {
            var sessionRepo = _unitOfWork.GetSessionRepository();
            var sessions = await sessionRepo.GetAllSessionsWithTrainerAndCategoryAsync();
            if (!sessions.Any()) return [];

            var sessionsdata = _mapper.Map<IEnumerable<SessionDTO>>(sessions).ToList();

            // One Grouped Count Query Covers Every Card, Rather Than One Round Trip Per Session.
            var bookedCounts = await _unitOfWork.GetBookingRepository().GetBookedCountsAsync(sessionsdata.Select(S => S.Id));
            foreach (var session in sessionsdata)
                session.AvailableSlots = session.Capacity - (bookedCounts.TryGetValue(session.Id, out var count) ? count : 0);

            IEnumerable<SessionDTO> filtered = sessionsdata;

            if (!string.IsNullOrWhiteSpace(search)) {
                var term = search.Trim();
                filtered = filtered.Where(S =>
                    S.CategoryName.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                    S.TrainerName.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                    S.Description.Contains(term, StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrWhiteSpace(status) && !status.Equals("all", StringComparison.OrdinalIgnoreCase))
                filtered = filtered.Where(S => S.Status.Equals(status, StringComparison.OrdinalIgnoreCase));

            return [.. filtered.OrderByDescending(S => S.StartDate)];
        }
        public async Task<SessionDTO?> GetSessionById(int sessionId) {
            var sessionRepo = _unitOfWork.GetSessionRepository();
            var session = await sessionRepo.GetSessionWithTrainerAndCategoryAsync(sessionId);
            if(session is null) return null;
            var sessiondata = _mapper.Map<SessionDTO>(session);
            // Assign Availble Slots To The Session
            sessiondata.AvailableSlots = sessiondata.Capacity - await sessionRepo.GetCountOfBookedSlotsAsync(sessionId);
            return sessiondata;
        }
        public async Task<bool> CreateSession(CreateSessionDTO createSessionDTO) {
            try {
                // Check If Trainer Exist
                if (!await IsTrainerExist(createSessionDTO.TrainerId)) return false;
                // Check If Category Exist
                if (!await IsCategoryExist(createSessionDTO.CategoryId)) return false;
                // Check If Date Time Is Valid
                if (!IsTimeValid(createSessionDTO.StartDate, createSessionDTO.EndDate)) return false;
                // Capacity Must Match The Database Check Constraint (Between 1 And 25).
                if (createSessionDTO.Capacity is > 25 or < 1) return false;
                // Create Session
                var sessionEntity = _mapper.Map<Session>(createSessionDTO);
                sessionEntity.CreatedAt = DateOnly.FromDateTime(DateTime.Now);
                sessionEntity.UpdatedAt = DateOnly.FromDateTime(DateTime.Now);
                await _unitOfWork.GetRepository<Session>().AddAsync(sessionEntity);
                return await _unitOfWork.SaveChangesAsync() > 0;
            } catch (Exception ex) {
                Console.WriteLine($"Creating Session Failed : {ex.ToString()}");
                return false;
            }
        }
        public async Task<UpdateSessionDTO?> GetSessionToUpdate(int sessionId) {
            var session = await _unitOfWork.GetSessionRepository().GetAsync(sessionId);
            if(!await IsSessionAllowedForUpdatingOrRemoving(session!)) return null;
            return _mapper.Map<UpdateSessionDTO>(session);
        }
        public async Task<bool> UpdateSession(UpdateSessionDTO updateSessionDTO, int sessionId) {
            try {
                var sessionRepo = _unitOfWork.GetSessionRepository();
                var session = await sessionRepo.GetAsync(sessionId);
                if (session is null) return false;
                if (!await IsSessionAllowedForUpdatingOrRemoving(session)) return false;
                if (!await IsTrainerExist(updateSessionDTO.TrainerId)) return false;
                if (!await IsCategoryExist(updateSessionDTO.CategoryId)) return false; 
                if (!IsTimeValid(updateSessionDTO.StartDate, updateSessionDTO.EndDate)) return false;
                _mapper.Map(updateSessionDTO, session);
                session.UpdatedAt = DateOnly.FromDateTime(DateTime.Now);
                sessionRepo.Update(session);
                return await _unitOfWork.SaveChangesAsync() > 0;
            } catch (Exception ex) {
                Console.WriteLine($"Updating Session Failed: {ex}");
                return false;
            }
        }
        public async Task<bool> RemoveSession(int sessionId) {
            try {
                var sessionRepo = _unitOfWork.GetSessionRepository();
                var session = await sessionRepo.GetAsync(sessionId);
                if (!await IsSessionAllowedForUpdatingOrRemoving(session!)) return false;
                sessionRepo.Delete(session!);
                return await _unitOfWork.SaveChangesAsync() > 0;
            } catch (Exception ex) {
                Console.WriteLine($"Removing Session Failed : {ex.ToString()}");
                return false;
            }
        }
        public async Task<IEnumerable<TrainerSelectDTO>> GetTrainersForDropdown() {
            var trainers = await _unitOfWork.GetRepository<Trainer>().GetAllAsync();
            return _mapper.Map<IEnumerable<TrainerSelectDTO>>(trainers);
        }
        public async Task<IEnumerable<CategorySelectDTO>> GetCategoriesForDropdown() {
            var categories = await _unitOfWork.GetRepository<Category>().GetAllAsync();
            return _mapper.Map<IEnumerable<CategorySelectDTO>>(categories);
        }

        #region Private Helper Methods
        private async Task<bool> IsTrainerExist(int trainerId)
            => await _unitOfWork.GetRepository<Trainer>().GetAsync(trainerId) is not null;
        private async Task<bool> IsCategoryExist(int categoryId)
            => await _unitOfWork.GetRepository<Category>().GetAsync(categoryId) is not null;
        private bool IsTimeValid(DateTime startDate, DateTime endDate)
            => endDate > startDate && startDate > DateTime.Now;
        private async Task<bool> IsSessionAllowedForUpdatingOrRemoving(Session session) {
            // Is Session Completed -> No Allow
            if (session.EndDate < DateTime.Now) return false;
            // Is Session Started -> No Allo
            if (session.StartDate <= DateTime.Now) return false;
            // Is Session Has Active Booking -> No Allow
            var hasActiveBooking = await _unitOfWork.GetSessionRepository().GetCountOfBookedSlotsAsync(session.Id) > 0;
            if (hasActiveBooking) return false;
            // All Ckecks true
            return true;
        }
        #endregion
    }
}
