using AutoMapper;
using Domin.Contract;
using Domin.GymEntities;
using Services.Abstraction.Contract;
using Shared.DTOs.SessionDTOs;
using Shared.DTOs.TrainerDTOs;
using Shared.Extensions;

namespace Services.Implmentations {
	public class TrainerService(IUnitOfWork unitOfWork, IMapper mapper) : ITrainerService {

		public async Task<IEnumerable<TrainerDTO>> GetAllTrainers(string? search = null, int? specialty = null) {
			var trainers = await unitOfWork.GetRepository<Trainer>().GetAllAsync();
			if (trainers is null || !trainers.Any()) return [];

			var result = mapper.Map<IEnumerable<TrainerDTO>>(trainers).ToList();

			// Workload Counts For Every Trainer In One Pass Over The Sessions Table.
			var now = DateTime.Now;
			var sessions = await unitOfWork.GetRepository<Session>().GetAllAsync();
			var byTrainer = sessions.GroupBy(S => S.TrainerId)
									.ToDictionary(G => G.Key, G => (Total: G.Count(), Upcoming: G.Count(S => S.StartDate > now)));

			foreach (var trainer in result) {
				if (!byTrainer.TryGetValue(trainer.Id, out var counts)) continue;
				trainer.SessionCount = counts.Total;
				trainer.UpcomingSessionCount = counts.Upcoming;
			}

			IEnumerable<TrainerDTO> filtered = result;

			if (!string.IsNullOrWhiteSpace(search)) {
				var term = search.Trim();
				filtered = filtered.Where(T =>
					T.Name.Contains(term, StringComparison.OrdinalIgnoreCase) ||
					T.Email.Contains(term, StringComparison.OrdinalIgnoreCase) ||
					T.Phone.Contains(term, StringComparison.OrdinalIgnoreCase) ||
					T.Specialties.Contains(term, StringComparison.OrdinalIgnoreCase));
			}

			if (specialty is > 0) {
				// The DTO Carries The Display Label, So Match On That Rather Than The Member Name.
				var wanted = ((Domin.Enums.Specialties)specialty.Value).GetDisplayName();
				filtered = filtered.Where(T => T.Specialties == wanted);
			}

			return [.. filtered.OrderBy(T => T.Name)];
		}

		public async Task<bool> CreateTrainer(CreateTrainerDTO createdTrainer) {
			try {
				if (await IsEmailExist(createdTrainer.Email) || await IsPhoneExist(createdTrainer.Phone)) return false;

				var trainer = mapper.Map<Trainer>(createdTrainer);
				trainer.CreatedAt = DateOnly.FromDateTime(DateTime.Now);
				trainer.UpdatedAt = DateOnly.FromDateTime(DateTime.Now);

				await unitOfWork.GetRepository<Trainer>().AddAsync(trainer);
				return await unitOfWork.SaveChangesAsync() > 0;
			} catch (Exception) {
				return false;
			}
		}

		public async Task<TrainerDTO?> GetTrainerDetails(int trainerId) {
			var trainer = await unitOfWork.GetRepository<Trainer>().GetAsync(trainerId);
			if (trainer is null) return null;

			var result = mapper.Map<TrainerDTO>(trainer);
			var sessions = await unitOfWork.GetRepository<Session>().GetAllAsync(S => S.TrainerId == trainerId);
			result.SessionCount = sessions.Count();
			result.UpcomingSessionCount = sessions.Count(S => S.StartDate > DateTime.Now);
			return result;
		}

		public async Task<TrainerToUpdateDTO?> GetTrainerToUpdate(int trainerId) {
			var trainer = await unitOfWork.GetRepository<Trainer>().GetAsync(trainerId);
			return trainer is null ? null : mapper.Map<TrainerToUpdateDTO>(trainer);
		}

		public async Task<bool> UpdateTrainerDetails(TrainerToUpdateDTO updatedTrainer, int trainerId) {
			try {
				var repo = unitOfWork.GetRepository<Trainer>();
				var trainerToUpdate = await repo.GetAsync(trainerId);

				if (trainerToUpdate is null
					|| await IsEmailExist(trainerId, updatedTrainer.Email)
					|| await IsPhoneExist(trainerId, updatedTrainer.Phone)) return false;

				trainerToUpdate.Email = updatedTrainer.Email;
				trainerToUpdate.Phone = updatedTrainer.Phone;
				trainerToUpdate.Photo = updatedTrainer.Photo;
				trainerToUpdate.Address.BuildingNumber = updatedTrainer.BuildingNumber;
				trainerToUpdate.Address.Street = updatedTrainer.Street;
				trainerToUpdate.Address.City = updatedTrainer.City;
				trainerToUpdate.Specialties = updatedTrainer.Specialties;
				trainerToUpdate.UpdatedAt = DateOnly.FromDateTime(DateTime.Now);

				repo.Update(trainerToUpdate);
				return await unitOfWork.SaveChangesAsync() > 0;
			} catch (Exception) {
				return false;
			}
		}

		public async Task<bool> RemoveTrainer(int trainerId) {
			try {
				var repo = unitOfWork.GetRepository<Trainer>();
				var trainerToRemove = await repo.GetAsync(trainerId);
				if (trainerToRemove is null || await HasActiveSessions(trainerId)) return false;

				repo.Delete(trainerToRemove);
				return await unitOfWork.SaveChangesAsync() > 0;
			} catch (Exception) {
				return false;
			}
		}

		public async Task<IEnumerable<SessionDTO>> GetTrainerSessions(int trainerId) {
			var sessions = await unitOfWork.GetSessionRepository().GetTrainerSessionsAsync(trainerId);
			var result = mapper.Map<IEnumerable<SessionDTO>>(sessions).ToList();

			var bookedCounts = await unitOfWork.GetBookingRepository().GetBookedCountsAsync(result.Select(S => S.Id));
			foreach (var session in result)
				session.AvailableSlots = session.Capacity - (bookedCounts.TryGetValue(session.Id, out var count) ? count : 0);

			return result;
		}

		public async Task<string?> GetTrainerPhoto(int trainerId)
			=> (await unitOfWork.GetRepository<Trainer>().GetAsync(trainerId))?.Photo;

		#region Helper Methods
		private async Task<bool> IsEmailExist(int trainerId, string email) {
			var trainerEmail = await unitOfWork.GetRepository<Trainer>().GetAllAsync(m => m.Email == email && m.Id != trainerId);
			return trainerEmail.Any();
		}
		private async Task<bool> IsEmailExist(string email) {
			var trainerEmail = await unitOfWork.GetRepository<Trainer>().GetAllAsync(m => m.Email == email);
			return trainerEmail.Any();
		}
		private async Task<bool> IsPhoneExist(int trainerId, string phone) {
			var trainerPhone = await unitOfWork.GetRepository<Trainer>().GetAllAsync(m => m.Phone == phone && m.Id != trainerId);
			return trainerPhone.Any();
		}
		private async Task<bool> IsPhoneExist(string phone) {
			var trainerPhone = await unitOfWork.GetRepository<Trainer>().GetAllAsync(m => m.Phone == phone);
			return trainerPhone.Any();
		}
		private async Task<bool> HasActiveSessions(int trainerId) {
			var activeSessions = await unitOfWork.GetRepository<Session>().GetAllAsync(s => s.TrainerId == trainerId && s.StartDate > DateTime.Now);
			return activeSessions.Any();
		}
		#endregion
	}
}
