using AutoMapper;
using Domin.GymEntities;
using Shared.DTOs.SessionDTOs;

namespace Services.Mapping {
    public class SessionProfile : Profile {
        public SessionProfile() {
            CreateMap<Session, SessionDTO>()
                .ForMember(dest => dest.CategoryName, opt => opt.MapFrom(src => src.SessionCategory.CategoryName))
                .ForMember(dest => dest.TrainerName, opt => opt.MapFrom(src => src.SessionTrainer.Name))
                // Resolved By The Service From The Bookings Table.
                .ForMember(dest => dest.AvailableSlots, opt => opt.Ignore());

            CreateMap<CreateSessionDTO, Session>();

            CreateMap<UpdateSessionDTO, Session>().ReverseMap();

            CreateMap<Category, CategorySelectDTO>();
        }
    }
}
