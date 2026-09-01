using Shared.DTOs.SessionDTOs;
using Shared.DTOs.TrainerDTOs;

namespace Services.Abstraction.Contract {
    public interface ITrainerService {
        Task<IEnumerable<TrainerDTO>> GetAllTrainers(string? search = null, int? specialty = null);
        Task<bool> CreateTrainer(CreateTrainerDTO createdTrainer);
        Task<TrainerDTO?> GetTrainerDetails(int trainerId);
        Task<TrainerToUpdateDTO?> GetTrainerToUpdate(int trainerId);
        Task<bool> UpdateTrainerDetails(TrainerToUpdateDTO updatedTrainer, int trainerId);
        Task<bool> RemoveTrainer(int trainerId);

        /// <summary>The Classes This Trainer Leads, Newest First.</summary>
        Task<IEnumerable<SessionDTO>> GetTrainerSessions(int trainerId);
        Task<string?> GetTrainerPhoto(int trainerId);
    }
}
