using Shared.DTOs.SessionDTOs;
using Shared.DTOs.TrainerDTOs;

namespace Services.Abstraction.Contract {
    public interface ISessionService {
        Task<IEnumerable<SessionDTO>> GetAllSessions(string? search = null, string? status = null);
        Task<SessionDTO?> GetSessionById(int sessionId);
        Task<bool> CreateSession(CreateSessionDTO createSessionDTO);
        Task<UpdateSessionDTO?> GetSessionToUpdate(int sessionId);
        Task<bool> UpdateSession(UpdateSessionDTO updateSessionDTO, int sessionId);
        Task<bool> RemoveSession(int sessionId);
        Task<IEnumerable<TrainerSelectDTO>> GetTrainersForDropdown();
        Task<IEnumerable<CategorySelectDTO>> GetCategoriesForDropdown();
    }
}
